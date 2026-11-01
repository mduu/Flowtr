using System.Text.Json;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Inbox.Mail;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Inbox;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Flowtr.Server.Features.Inbox.Queries;

/// <summary>
/// The receipts in the inbox, newest mail first, and the state of the mailbox.
/// </summary>
public sealed record GetInbox;

public sealed class GetInboxHandler(ApplicationDbContext db, MailboxState state, IOptions<MailboxOptions> options) : IQueryHandler<GetInbox, InboxView>
{
    public async Task<Result<InboxView>> Handle(GetInbox query, CancellationToken cancellationToken)
    {
        var items = await db.InboxItems.AsNoTracking()
            .Include(i => i.Mail)
            .Include(i => i.Files)
            .OrderByDescending(i => i.Mail!.ReceivedAt)
            .ToListAsync(cancellationToken);

        return Result.Success(new InboxView(
            items.Select(i => new InboxItemView(
                i.Id,
                i.Files.OrderBy(f => f.Position).Select(f => new UploadedReceipt(f.UploadId, f.FileName, f.ContentType, f.Size)).ToList(),
                i.Mail!.Sender, i.Mail.OriginalSender, i.Mail.Subject, i.Mail.ReceivedAt,
                i.ImportedAt + ReceiptHousekeeping.UploadLifetime,
                i.Analysis, i.AnalysisError,
                i.Proposal is null ? null : JsonSerializer.Deserialize<BookingProposal>(i.Proposal)))
                .ToList(),
            Status(options.Value, state)));
    }

    public static MailboxStatus Status(MailboxOptions options, MailboxState state) =>
        new(options.IsConfigured, options.ShownAddress, state.LastFetch, options.IsConfigured ? state.LastError : null);
}
