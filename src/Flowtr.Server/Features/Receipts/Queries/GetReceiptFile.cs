using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Receipts.Queries;

/// <summary>
/// A file of a filed receipt, named by its number (for example <c>A-2026-007.pdf</c>, from the second file on <c>A-2026-007_2.pdf</c>).
/// </summary>
public sealed record GetReceiptFile(Guid FileId);

public sealed class GetReceiptFileHandler(ApplicationDbContext db, IReceiptStore store) : IQueryHandler<GetReceiptFile, FileContent>
{
    public async Task<Result<FileContent>> Handle(GetReceiptFile query, CancellationToken cancellationToken)
    {
        var file = await db.ReceiptFiles.AsNoTracking()
            .Where(f => f.Id == query.FileId)
            .Select(f => new { f.BlobName, f.ContentType, f.Position, Number = db.Receipts.Where(r => r.Id == f.ReceiptId).Select(r => r.Number).First() })
            .FirstOrDefaultAsync(cancellationToken);

        if (file is null || await store.OpenReceipt(file.BlobName, cancellationToken) is not { } content)
        {
            return Result.Failure<FileContent>("Diesen Beleg gibt es nicht.");
        }

        var name = file.Position > 1 ? $"{file.Number}_{file.Position}" : file.Number;
        return Result.Success(new FileContent(content, file.ContentType, name + Path.GetExtension(file.BlobName)));
    }
}

/// <summary>
/// The caller disposes <see cref="Content"/>.
/// </summary>
public sealed record FileContent(Stream Content, string ContentType, string FileName);
