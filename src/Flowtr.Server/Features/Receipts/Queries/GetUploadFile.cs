using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Shared;

namespace Flowtr.Server.Features.Receipts.Queries;

/// <summary>
/// The file of an upload that no booking uses yet, for the preview in the booking dialog.
/// </summary>
public sealed record GetUploadFile(Guid UploadId);

public sealed class GetUploadFileHandler(IReceiptStore store) : IQueryHandler<GetUploadFile, FileContent>
{
    public async Task<Result<FileContent>> Handle(GetUploadFile query, CancellationToken cancellationToken)
    {
        if (await store.GetUpload(query.UploadId, cancellationToken) is not { } upload
            || await store.OpenUpload(query.UploadId, cancellationToken) is not { } content)
        {
            return Result.Failure<FileContent>(ReceiptFiling.UploadGone);
        }

        return Result.Success(new FileContent(content, upload.ContentType, upload.FileName));
    }
}
