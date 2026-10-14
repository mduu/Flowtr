using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Flowtr.Server.Features.Receipts;

namespace Flowtr.Tests.Receipts;

/// <summary>
/// <see cref="IReceiptStore"/> in memory, for tests.
/// </summary>
internal sealed class MemoryReceiptStore : IReceiptStore
{
    public ConcurrentDictionary<Guid, (byte[] Content, UploadInfo Info)> Uploads { get; } = new();

    public ConcurrentDictionary<string, byte[]> Receipts { get; } = new();

    public async Task SaveUpload(Guid uploadId, Stream content, UploadInfo info, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        Uploads[uploadId] = (copy.ToArray(), info);
    }

    public Task<UploadInfo?> GetUpload(Guid uploadId, CancellationToken cancellationToken) =>
        Task.FromResult(Uploads.TryGetValue(uploadId, out var upload) ? upload.Info : null);

    public Task<Stream?> OpenUpload(Guid uploadId, CancellationToken cancellationToken) =>
        Task.FromResult<Stream?>(Uploads.TryGetValue(uploadId, out var upload) ? new MemoryStream(upload.Content) : null);

    public Task DeleteUpload(Guid uploadId, CancellationToken cancellationToken)
    {
        Uploads.TryRemove(uploadId, out _);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<(Guid UploadId, DateTimeOffset UploadedAt)> ListUploads([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var (id, upload) in Uploads)
        {
            yield return (id, upload.Info.UploadedAt);
        }

        await Task.CompletedTask;
    }

    public Task<bool> CopyToReceipts(Guid uploadId, string blobName, string contentType, bool overwrite, CancellationToken cancellationToken)
    {
        if (overwrite)
        {
            Receipts[blobName] = Uploads[uploadId].Content;
            return Task.FromResult(true);
        }

        return Task.FromResult(Receipts.TryAdd(blobName, Uploads[uploadId].Content));
    }

    public Task<Stream?> OpenReceipt(string blobName, CancellationToken cancellationToken) =>
        Task.FromResult<Stream?>(Receipts.TryGetValue(blobName, out var content) ? new MemoryStream(content) : null);

    public Task DeleteReceipt(string blobName, CancellationToken cancellationToken)
    {
        Receipts.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> ListReceipts([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var name in Receipts.Keys)
        {
            yield return name;
        }

        await Task.CompletedTask;
    }
}
