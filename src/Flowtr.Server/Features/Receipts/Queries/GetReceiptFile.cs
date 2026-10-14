using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts.Model;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Receipts.Queries;

/// <summary>
/// The file of a filed receipt, named by its number (for example <c>A-2026-007.pdf</c>).
/// </summary>
public sealed record GetReceiptFile(Guid ReceiptId);

public sealed class GetReceiptFileHandler(ApplicationDbContext db, IReceiptStore store) : IQueryHandler<GetReceiptFile, ReceiptFile>
{
    public async Task<Result<ReceiptFile>> Handle(GetReceiptFile query, CancellationToken cancellationToken)
    {
        if (await db.Set<Receipt>().FindAsync([query.ReceiptId], cancellationToken) is not { } receipt
            || await store.OpenReceipt(receipt.BlobName, cancellationToken) is not { } content)
        {
            return Result.Failure<ReceiptFile>("Diesen Beleg gibt es nicht.");
        }

        return Result.Success(new ReceiptFile(content, receipt.ContentType, receipt.Number + Path.GetExtension(receipt.BlobName)));
    }
}

/// <summary>
/// The caller disposes <see cref="Content"/>.
/// </summary>
public sealed record ReceiptFile(Stream Content, string ContentType, string FileName);
