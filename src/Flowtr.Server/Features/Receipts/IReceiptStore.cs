namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// Files of receipts. Uploads wait in <c>eingang</c> until a booking uses them. Filed receipts are in <c>belege</c> (ADR-001).
/// </summary>
public interface IReceiptStore
{
    Task SaveUpload(Guid uploadId, Stream content, UploadInfo info, CancellationToken cancellationToken);

    Task<UploadInfo?> GetUpload(Guid uploadId, CancellationToken cancellationToken);

    Task<Stream?> OpenUpload(Guid uploadId, CancellationToken cancellationToken);

    Task DeleteUpload(Guid uploadId, CancellationToken cancellationToken);

    /// <summary>
    /// Uploads with their upload time, for the cleanup of old uploads.
    /// </summary>
    IAsyncEnumerable<(Guid UploadId, DateTimeOffset UploadedAt)> ListUploads(CancellationToken cancellationToken);

    /// <summary>
    /// Copies an upload to <c>belege</c>. The upload stays: delete it only after the booking is saved.
    /// </summary>
    /// <param name="overwrite">Only to replace the file of the same receipt. Else an existing blob is never overwritten.</param>
    /// <returns>False if a blob with this name exists and <paramref name="overwrite"/> is false.</returns>
    Task<bool> CopyToReceipts(Guid uploadId, string blobName, string contentType, bool overwrite, CancellationToken cancellationToken);

    Task<Stream?> OpenReceipt(string blobName, CancellationToken cancellationToken);

    Task DeleteReceipt(string blobName, CancellationToken cancellationToken);

    IAsyncEnumerable<string> ListReceipts(CancellationToken cancellationToken);
}

/// <summary>
/// What Flowtr knows about an upload. <see cref="Sha256"/> is hex.
/// </summary>
public sealed record UploadInfo(string FileName, string ContentType, long Size, string Sha256, DateTimeOffset UploadedAt);
