using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// <see cref="IReceiptStore"/> on Azure Blob Storage (locally Azurite). The upload metadata is in blob metadata.
/// </summary>
public sealed class BlobReceiptStore(BlobServiceClient blobs) : IReceiptStore
{
    public const string Uploads = "eingang";

    public const string Receipts = "belege";

    private BlobContainerClient UploadContainer => blobs.GetBlobContainerClient(Uploads);

    private BlobContainerClient ReceiptContainer => blobs.GetBlobContainerClient(Receipts);

    /// <summary>
    /// Makes the containers if they do not exist. In Azure, the infrastructure (Bicep) makes them with their policies.
    /// </summary>
    public async Task EnsureContainers(CancellationToken cancellationToken)
    {
        await UploadContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await ReceiptContainer.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task SaveUpload(Guid uploadId, Stream content, UploadInfo info, CancellationToken cancellationToken)
    {
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = info.ContentType },
            Metadata = new Dictionary<string, string>
            {
                ["filename"] = Uri.EscapeDataString(info.FileName),
                ["sha256"] = info.Sha256,
                ["uploadedat"] = info.UploadedAt.ToString("O"),
            },
        };

        await UploadContainer.GetBlobClient(uploadId.ToString()).UploadAsync(content, options, cancellationToken);
    }

    public async Task<UploadInfo?> GetUpload(Guid uploadId, CancellationToken cancellationToken)
    {
        try
        {
            var properties = (await UploadContainer.GetBlobClient(uploadId.ToString()).GetPropertiesAsync(cancellationToken: cancellationToken)).Value;
            return new UploadInfo(
                Uri.UnescapeDataString(Meta(properties.Metadata, "filename") ?? "beleg"),
                properties.ContentType,
                properties.ContentLength,
                Meta(properties.Metadata, "sha256") ?? "",
                DateTimeOffset.TryParse(Meta(properties.Metadata, "uploadedat"), out var at) ? at : properties.CreatedOn);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public Task<Stream?> OpenUpload(Guid uploadId, CancellationToken cancellationToken) =>
        Open(UploadContainer.GetBlobClient(uploadId.ToString()), cancellationToken);

    public Task DeleteUpload(Guid uploadId, CancellationToken cancellationToken) =>
        UploadContainer.GetBlobClient(uploadId.ToString()).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async IAsyncEnumerable<(Guid UploadId, DateTimeOffset UploadedAt)> ListUploads([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in UploadContainer.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (Guid.TryParse(blob.Name, out var id))
            {
                yield return (id, blob.Properties.CreatedOn ?? DateTimeOffset.MinValue);
            }
        }
    }

    public async Task<bool> CopyToReceipts(Guid uploadId, string blobName, string contentType, bool overwrite, CancellationToken cancellationToken)
    {
        await using var content = await OpenUpload(uploadId, cancellationToken)
            ?? throw new InvalidOperationException($"Upload {uploadId} not found.");

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Conditions = overwrite ? null : new BlobRequestConditions { IfNoneMatch = ETag.All },
        };

        try
        {
            await ReceiptContainer.GetBlobClient(blobName).UploadAsync(content, options, cancellationToken);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status is 409 or 412)
        {
            return false;
        }
    }

    public Task<Stream?> OpenReceipt(string blobName, CancellationToken cancellationToken) =>
        Open(ReceiptContainer.GetBlobClient(blobName), cancellationToken);

    public Task DeleteReceipt(string blobName, CancellationToken cancellationToken) =>
        ReceiptContainer.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async IAsyncEnumerable<string> ListReceipts([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in ReceiptContainer.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            yield return blob.Name;
        }
    }

    private static string? Meta(IDictionary<string, string> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value : null;

    private static async Task<Stream?> Open(BlobClient blob, CancellationToken cancellationToken)
    {
        try
        {
            return await blob.OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }
}
