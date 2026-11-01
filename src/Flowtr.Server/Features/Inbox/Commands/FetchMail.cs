using System.Security.Authentication;
using System.Security.Cryptography;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Server.Features.Inbox.Model;
using Flowtr.Server.Features.Receipts;
using Flowtr.Server.Features.Receipts.Model;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Flowtr.Server.Features.Inbox.Commands;

/// <summary>
/// Reads the mailbox and puts the receipts of allowed mails into the inbox (story #010, ADR-009).
/// Order per mail: files to <c>eingang</c>, then the database, then the mail to «Flowtr/Verarbeitet» or «Flowtr/Abgelehnt».
/// </summary>
/// <remarks>
/// A mail that fails stays in the mailbox: the next fetch tries it again. A mail that Flowtr knows (<see cref="ImportedMail"/>)
/// is only moved: the move after the last import failed.
/// </remarks>
public sealed record FetchMail;

public sealed class FetchMailHandler(
    ApplicationDbContext db,
    IMailbox mailbox,
    IReceiptStore store,
    SenderCheck senders,
    MailboxState state,
    IOptions<MailboxOptions> options,
    TimeProvider time,
    ILogger<FetchMailHandler> logger)
    : ICommandHandler<FetchMail, Result<FetchResult>>
{
    public const string NotConfigured = "Die Mailbox ist nicht eingerichtet.";

    public const string Unreachable = "Die Mailbox ist nicht erreichbar. Flowtr versucht es beim nächsten Abholen wieder.";

    public async Task<Result<FetchResult>> Handle(FetchMail command, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return Result.Failure<FetchResult>(NotConfigured);
        }

        await state.Lock.WaitAsync(cancellationToken);
        try
        {
            return await Fetch(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Fetching mail from {Host} failed", options.Value.Host);
            state.Failed(Unreachable);
            return Result.Failure<FetchResult>(Unreachable);
        }
        finally
        {
            state.Lock.Release();
        }
    }

    private async Task<Result<FetchResult>> Fetch(CancellationToken cancellationToken)
    {
        IMailboxSession session;
        try
        {
            session = await mailbox.Open(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Mailbox {Host} not reachable", options.Value.Host);
            var error = exception is AuthenticationException or MailKit.Security.AuthenticationException
                ? "Die Anmeldung an der Mailbox ist fehlgeschlagen. Bitte Benutzer und Passwort prüfen."
                : Unreachable;
            state.Failed(error);
            return Result.Failure<FetchResult>(error);
        }

        var (receipts, rejected) = (0, 0);
        await using (session)
        {
            foreach (var id in await session.List(cancellationToken))
            {
                try
                {
                    var message = await session.Get(id, cancellationToken);
                    var (folder, imported) = await Import(message, cancellationToken);
                    await session.Move(id, folder, cancellationToken);
                    receipts += imported;
                    rejected += folder == options.Value.RejectedFolder ? 1 : 0;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Mail {Id} not imported, it stays in the mailbox", id);
                    db.ChangeTracker.Clear();
                }
            }
        }

        state.Succeeded(time.GetUtcNow());
        if (receipts > 0 || rejected > 0)
        {
            logger.LogInformation("Fetched mail: {Receipts} receipts, {Rejected} mails rejected", receipts, rejected);
        }

        return Result.Success(new FetchResult(receipts, rejected));
    }

    /// <returns>The folder to move the mail to and the number of new receipts.</returns>
    private async Task<(string Folder, int Receipts)> Import(MimeMessage message, CancellationToken cancellationToken)
    {
        var key = MailReceipts.MessageKey(message);
        if (await db.ImportedMails.AsNoTracking().FirstOrDefaultAsync(m => m.MessageKey == key, cancellationToken) is { } known)
        {
            return (Folder(known.Outcome), 0);
        }

        var now = time.GetUtcNow();
        var mail = new ImportedMail
        {
            MessageKey = key,
            MessageId = message.MessageId is { } messageId ? MailReceipts.Shorten(messageId, 300) : null,
            Sender = MailReceipts.Shorten(MailReceipts.Sender(message) ?? message.From.ToString(), 256),
            OriginalSender = MailReceipts.OriginalSender(message),
            Subject = MailReceipts.Shorten(message.Subject ?? "", 500),
            ReceivedAt = message.Date == DateTimeOffset.MinValue ? now : message.Date,
            ImportedAt = now,
        };
        db.ImportedMails.Add(mail);

        if (await senders.Reject(message, cancellationToken) is { } reason)
        {
            mail.Outcome = MailOutcome.Rejected;
            mail.Reason = reason;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Rejected mail from {Sender}: {Reason}", mail.Sender, reason);
            return (options.Value.RejectedFolder, 0);
        }

        // One item with all files of the mail: one receipt, one booking (story #012).
        var item = new InboxItem { MailId = mail.Id, ImportedAt = mail.ImportedAt, Analysis = AnalysisState.Pending };
        item.Files = await SaveUploads(mail, item, MailReceipts.Files(message), cancellationToken);
        mail.Outcome = item.Files.Count > 0 ? MailOutcome.Imported : MailOutcome.Duplicate;
        if (item.Files.Count > 0)
        {
            db.InboxItems.Add(item);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var file in item.Files)
            {
                await store.DeleteUpload(file.UploadId, CancellationToken.None);
            }

            throw;
        }

        var receipts = item.Files.Count > 0 ? 1 : 0;
        logger.LogInformation("Imported mail {Id}: {Count} files", mail.Id, item.Files.Count);
        return (options.Value.ProcessedFolder, receipts);
    }

    /// <summary>
    /// Puts each new file into <c>eingang</c>, at most <see cref="Receipt.MaxFiles"/>. A file that is in the inbox or filed already is left out.
    /// </summary>
    private async Task<List<InboxFile>> SaveUploads(ImportedMail mail, InboxItem item, IReadOnlyList<MailFile> files, CancellationToken cancellationToken)
    {
        var saved = new List<InboxFile>();
        foreach (var file in files)
        {
            var sha256 = Convert.ToHexStringLower(SHA256.HashData(file.Content));
            if (saved.Any(f => f.Sha256 == sha256) || await IsKnown(sha256, cancellationToken))
            {
                logger.LogInformation("A file of mail {Id} is known already", mail.Id);
                continue;
            }

            if (saved.Count == Receipt.MaxFiles)
            {
                logger.LogWarning("Mail {Id} has more than {Max} files: Flowtr takes the first ones", mail.Id, Receipt.MaxFiles);
                break;
            }

            var inboxFile = new InboxFile
            {
                InboxItemId = item.Id,
                Position = saved.Count + 1,
                UploadId = Guid.CreateVersion7(),
                FileName = MailReceipts.Shorten(file.FileName, 255),
                ContentType = file.ContentType,
                Size = file.Content.Length,
                Sha256 = sha256,
            };
            await store.SaveUpload(inboxFile.UploadId, new MemoryStream(file.Content),
                new UploadInfo(inboxFile.FileName, inboxFile.ContentType, inboxFile.Size, sha256, mail.ImportedAt), cancellationToken);
            saved.Add(inboxFile);
        }

        return saved;
    }

    private async Task<bool> IsKnown(string sha256, CancellationToken cancellationToken) =>
        await db.InboxFiles.AnyAsync(f => f.Sha256 == sha256, cancellationToken)
        || await db.ReceiptFiles.AnyAsync(f => f.Sha256 == sha256, cancellationToken);

    private string Folder(MailOutcome outcome) =>
        outcome == MailOutcome.Rejected ? options.Value.RejectedFolder : options.Value.ProcessedFolder;
}
