using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Server.Features.Receipts.Model;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// Files the receipt of a booking: number, blobs in <c>belege</c>, <see cref="Receipt"/> with its files (ADR-001, ADR-007, story #012).
/// Order: first the blobs, then the database. On a failure, the new blobs are deleted again.
/// </summary>
public sealed class ReceiptFiling(ApplicationDbContext db, IReceiptStore store, TimeProvider time, ILogger<ReceiptFiling> logger)
{
    public const string UploadGone = "Der hochgeladene Beleg ist nicht mehr da. Bitte lade ihn nochmals hoch.";

    public static readonly string TooManyFiles = $"Ein Beleg hat höchstens {Receipt.MaxFiles} Dateien.";

    private const int Attempts = 5;

    /// <summary>
    /// Saves <paramref name="booking"/> (already tracked) with the changes of its receipt: the uploads become new files,
    /// the removed files go. A receipt without files goes too. A new receipt gets the next number of its year and kind.
    /// An existing receipt keeps its number.
    /// </summary>
    public async Task<Result> Save(Booking booking, IReadOnlyList<Guid> uploadIds, IReadOnlyList<Guid> removedFileIds, CancellationToken cancellationToken)
    {
        var uploads = new List<(Guid Id, UploadInfo Info)>();
        foreach (var uploadId in uploadIds.Distinct())
        {
            if (await store.GetUpload(uploadId, cancellationToken) is not { } upload)
            {
                return Result.Failure(UploadGone);
            }

            uploads.Add((uploadId, upload));
        }

        var existing = await Load(booking, cancellationToken);
        var removed = existing?.Files.Where(f => removedFileIds.Contains(f.Id)).ToList() ?? [];
        var remaining = (existing?.Files.Count ?? 0) - removed.Count;
        if (remaining + uploads.Count > Receipt.MaxFiles)
        {
            return Result.Failure(TooManyFiles);
        }

        var blobsToDelete = removed.Select(f => f.BlobName).ToList();
        db.ReceiptFiles.RemoveRange(removed);

        // Without files, or in another year or kind (only possible without remaining files): the receipt goes.
        if (existing is not null && (remaining + uploads.Count == 0 || existing.Year != booking.Date.Year || existing.Kind != booking.Kind))
        {
            blobsToDelete.AddRange(Detach(booking, existing).Except(blobsToDelete));
            existing = null;
        }

        if (uploads.Count == 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await DeleteAfterSave(blobsToDelete, cancellationToken);
            return Result.Success();
        }

        await TakeFromInbox(uploads.Select(u => u.Id).ToList(), cancellationToken);

        for (var attempt = 1; ; attempt++)
        {
            var (receipt, sequence) = existing is null ? await NewReceipt(booking, cancellationToken) : (existing, null);
            var added = AddFiles(booking, receipt, uploads);

            // Never overwrite the file of another receipt: a taken name means a taken number.
            var copied = await Copy(added, uploads, cancellationToken);
            if (copied.Count < added.Count)
            {
                await DeleteAfterSave(copied, cancellationToken);
                if (attempt == Attempts || sequence is null)
                {
                    return Result.Failure($"Der Beleg {receipt.Number} existiert schon. Bitte versuche es nochmals.");
                }

                logger.LogInformation("Receipt file {Number} exists, attempt {Attempt}", receipt.Number, attempt);
                await Reset(receipt, added, sequence, cancellationToken);
                continue;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (attempt < Attempts && sequence is not null && exception.Entries.All(entry => entry.Entity is ReceiptSequence))
            {
                // Another save took the number at the same time: try again with the next one.
                logger.LogInformation("Receipt number {Number} taken in parallel, attempt {Attempt}", receipt.Number, attempt);
                await DeleteAfterSave(copied, cancellationToken);
                await Reset(receipt, added, sequence, cancellationToken);
                continue;
            }
            catch
            {
                await DeleteAfterSave(copied, cancellationToken);
                throw;
            }

            foreach (var (uploadId, _) in uploads)
            {
                await store.DeleteUpload(uploadId, cancellationToken);
            }

            await DeleteAfterSave(blobsToDelete, cancellationToken);
            logger.LogInformation("Filed receipt {Number} with {Count} new files", receipt.Number, added.Count);
            return Result.Success();
        }
    }

    /// <summary>
    /// Removes the receipt of <paramref name="booking"/> with its files from the database. Gives the blobs to delete after the save.
    /// </summary>
    public async Task<IReadOnlyList<string>> Detach(Booking booking, CancellationToken cancellationToken) =>
        await Load(booking, cancellationToken) is { } receipt ? Detach(booking, receipt) : [];

    /// <summary>
    /// Deletes blobs after the database save. A failure only goes to the log: the housekeeping job finds the blob later.
    /// </summary>
    public async Task DeleteAfterSave(IEnumerable<string> blobNames, CancellationToken cancellationToken)
    {
        foreach (var blobName in blobNames)
        {
            try
            {
                await store.DeleteReceipt(blobName, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Receipt blob {BlobName} not deleted", blobName);
            }
        }
    }

    /// <summary>
    /// The year and kind of a booking with a receipt must not change: they are part of the receipt number.
    /// Possible only when all files of the receipt go.
    /// </summary>
    public async Task<string?> CheckChange(Booking booking, BookingInput input, CancellationToken cancellationToken)
    {
        if ((booking.Date.Year == input.Date.Year && booking.Kind == input.Kind) || await Load(booking, cancellationToken) is not { } receipt)
        {
            return null;
        }

        var removed = input.RemovedFileIds ?? [];
        return receipt.Files.All(f => removed.Contains(f.Id))
            ? null
            : "Jahr und Art einer Buchung mit Beleg lassen sich nicht ändern: Sie sind Teil der Belegnummer. Entferne zuerst die Dateien des Belegs.";
    }

    public static string Number(BookingKind kind, int year, int sequenceNumber) =>
        $"{(kind == BookingKind.Income ? "E" : "A")}-{year}-{sequenceNumber:000}";

    private async Task<Receipt?> Load(Booking booking, CancellationToken cancellationToken) =>
        booking.ReceiptId is { } id
            ? await db.Receipts.Include(r => r.Files).FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            : null;

    private List<string> Detach(Booking booking, Receipt receipt)
    {
        booking.ReceiptId = null;
        db.ReceiptFiles.RemoveRange(receipt.Files);
        db.Receipts.Remove(receipt);
        return receipt.Files.Select(f => f.BlobName).ToList();
    }

    /// <summary>
    /// Receipt files from the e-mail inbox are booked with this save: their item leaves the inbox in the same transaction.
    /// </summary>
    private async Task TakeFromInbox(IReadOnlyList<Guid> uploadIds, CancellationToken cancellationToken)
    {
        var items = await db.InboxItems.Include(i => i.Files)
            .Where(i => i.Files.Any(f => uploadIds.Contains(f.UploadId)))
            .ToListAsync(cancellationToken);
        db.InboxItems.RemoveRange(items);
    }

    private List<ReceiptFile> AddFiles(Booking booking, Receipt receipt, IReadOnlyList<(Guid Id, UploadInfo Info)> uploads)
    {
        var added = new List<ReceiptFile>();
        foreach (var (_, upload) in uploads)
        {
            var position = ++receipt.LastFilePosition;
            var file = new ReceiptFile
            {
                ReceiptId = receipt.Id,
                Position = position,
                BlobName = ReceiptFiles.BlobName(booking.Date.Year, booking.Kind == BookingKind.Income, receipt.Number, booking.Description, Extension(upload.ContentType), position),
                FileName = upload.FileName,
                ContentType = upload.ContentType,
                Size = upload.Size,
                Sha256 = upload.Sha256,
                FiledAt = time.GetUtcNow(),
            };
            db.ReceiptFiles.Add(file);
            added.Add(file);
        }

        return added;
    }

    /// <returns>The blobs that were copied. Fewer than <paramref name="files"/>: a name was taken.</returns>
    private async Task<List<string>> Copy(List<ReceiptFile> files, IReadOnlyList<(Guid Id, UploadInfo Info)> uploads, CancellationToken cancellationToken)
    {
        var copied = new List<string>();
        for (var i = 0; i < files.Count; i++)
        {
            if (!await store.CopyToReceipts(uploads[i].Id, files[i].BlobName, files[i].ContentType, overwrite: false, cancellationToken))
            {
                break;
            }

            copied.Add(files[i].BlobName);
        }

        return copied;
    }

    private async Task<(Receipt Receipt, ReceiptSequence Sequence)> NewReceipt(Booking booking, CancellationToken cancellationToken)
    {
        var year = booking.Date.Year;
        var sequence = await db.Set<ReceiptSequence>().FindAsync([year, booking.Kind], cancellationToken);
        if (sequence is null)
        {
            sequence = new ReceiptSequence { Year = year, Kind = booking.Kind };
            db.Set<ReceiptSequence>().Add(sequence);
        }

        sequence.LastNumber++;

        var receipt = new Receipt
        {
            Year = year,
            Kind = booking.Kind,
            SequenceNumber = sequence.LastNumber,
            Number = Number(booking.Kind, year, sequence.LastNumber),
        };
        db.Receipts.Add(receipt);
        booking.ReceiptId = receipt.Id;

        return (receipt, sequence);
    }

    private async Task Reset(Receipt receipt, List<ReceiptFile> files, ReceiptSequence? sequence, CancellationToken cancellationToken)
    {
        foreach (var file in files)
        {
            db.Entry(file).State = EntityState.Detached;
        }

        db.Entry(receipt).State = EntityState.Detached;

        if (sequence is not null)
        {
            var entry = db.Entry(sequence);
            if (entry.State == EntityState.Added)
            {
                entry.State = EntityState.Detached;
            }
            else
            {
                await entry.ReloadAsync(cancellationToken);
            }
        }
    }

    private static string Extension(string contentType) => contentType switch
    {
        "application/pdf" => ".pdf",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        MailContentTypes.Mail => ".eml",
        ReceiptContentTypes.Table => ".csv",
        _ => "",
    };
}
