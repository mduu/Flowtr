using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Inbox.Commands;

/// <summary>
/// Removes a receipt from the inbox, for example advertising or a duplicate. The mail stays in «Flowtr/Verarbeitet».
/// </summary>
public sealed record DiscardInboxItem(Guid Id);

public sealed class DiscardInboxItemHandler(ApplicationDbContext db, IReceiptStore store, ILogger<DiscardInboxItemHandler> logger)
    : ICommandHandler<DiscardInboxItem, Result>
{
    public async Task<Result> Handle(DiscardInboxItem command, CancellationToken cancellationToken)
    {
        if (await db.InboxItems.FindAsync([command.Id], cancellationToken) is not { } item)
        {
            return Result.Failure("Diesen Beleg gibt es im Eingang nicht mehr.");
        }

        db.InboxItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await store.DeleteUpload(item.UploadId, cancellationToken);

        logger.LogInformation("Discarded inbox receipt {Id}", item.Id);
        return Result.Success();
    }
}
