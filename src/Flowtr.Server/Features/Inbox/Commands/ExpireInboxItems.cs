using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Inbox.Commands;

/// <summary>
/// Removes receipts that are in the inbox longer than <see cref="ReceiptHousekeeping.UploadLifetime"/> (30 days):
/// the housekeeping deletes their uploads at the same age.
/// </summary>
public sealed record ExpireInboxItems;

public sealed class ExpireInboxItemsHandler(ApplicationDbContext db, IReceiptStore store, TimeProvider time, ILogger<ExpireInboxItemsHandler> logger)
    : ICommandHandler<ExpireInboxItems, Result<int>>
{
    public async Task<Result<int>> Handle(ExpireInboxItems command, CancellationToken cancellationToken)
    {
        var limit = time.GetUtcNow() - ReceiptHousekeeping.UploadLifetime;
        var expired = await db.InboxItems.Include(i => i.Files).Where(i => i.ImportedAt <= limit).ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return Result.Success(0);
        }

        db.InboxFiles.RemoveRange(expired.SelectMany(i => i.Files));
        db.InboxItems.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var file in expired.SelectMany(i => i.Files))
        {
            await store.DeleteUpload(file.UploadId, cancellationToken);
        }

        logger.LogInformation("Removed {Count} expired inbox receipts", expired.Count);
        return Result.Success(expired.Count);
    }
}
