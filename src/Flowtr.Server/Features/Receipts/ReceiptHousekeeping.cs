using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts.Model;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// At start and then once a day: makes the containers, deletes uploads older than 30 days and reports
/// receipts without a blob and blobs without a receipt (ADR-001: the save can fail between blob and database).
/// </summary>
public sealed class ReceiptHousekeeping(IServiceScopeFactory scopes, TimeProvider time, ILogger<ReceiptHousekeeping> logger) : BackgroundService
{
    public static readonly TimeSpan UploadLifetime = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));

        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await Run(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Receipt housekeeping failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// One run. Public for tests.
    /// </summary>
    public async Task Run(IServiceProvider services, CancellationToken cancellationToken)
    {
        var store = services.GetRequiredService<IReceiptStore>();
        if (store is BlobReceiptStore blobs)
        {
            await blobs.EnsureContainers(cancellationToken);
        }

        var limit = time.GetUtcNow() - UploadLifetime;
        await foreach (var (uploadId, uploadedAt) in store.ListUploads(cancellationToken))
        {
            if (uploadedAt <= limit)
            {
                await store.DeleteUpload(uploadId, cancellationToken);
                logger.LogInformation("Deleted old upload {UploadId}", uploadId);
            }
        }

        var db = services.GetRequiredService<ApplicationDbContext>();
        var known = (await db.Set<Receipt>().Select(r => r.BlobName).ToListAsync(cancellationToken)).ToHashSet();
        var stored = new HashSet<string>();
        await foreach (var blobName in store.ListReceipts(cancellationToken))
        {
            stored.Add(blobName);
            if (!known.Contains(blobName))
            {
                logger.LogWarning("Receipt blob {BlobName} has no receipt in the database", blobName);
            }
        }

        foreach (var missing in known.Except(stored))
        {
            logger.LogError("Receipt {BlobName} is in the database, but its blob is missing", missing);
        }
    }
}
