using System.Security.Cryptography;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Receipts.Commands;

/// <summary>
/// Checks a file and puts it into <c>eingang</c>. A booking files it later (<see cref="ReceiptFiling"/>).
/// </summary>
public sealed record UploadReceipt(Stream Content, string FileName);

public sealed class UploadReceiptHandler(IReceiptStore store, TimeProvider time, ILogger<UploadReceiptHandler> logger)
    : ICommandHandler<UploadReceipt, Result<UploadedReceipt>>
{
    public async Task<Result<UploadedReceipt>> Handle(UploadReceipt command, CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await command.Content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (content.Length + read > ReceiptFiles.MaxSize)
            {
                return Result.Failure<UploadedReceipt>("Die Datei ist zu gross (höchstens 20 MB).");
            }

            content.Write(buffer, 0, read);
        }

        if (content.Length == 0)
        {
            return Result.Failure<UploadedReceipt>("Die Datei ist leer.");
        }

        if (ReceiptFiles.Detect(content.GetBuffer().AsSpan(0, (int)Math.Min(content.Length, 32))) is not var (contentType, _))
        {
            return Result.Failure<UploadedReceipt>("Flowtr nimmt nur PDF und Bilder (JPEG, PNG, HEIC, WebP) als Beleg.");
        }

        content.Position = 0;
        var sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(content, cancellationToken));
        content.Position = 0;

        var uploadId = Guid.CreateVersion7();
        var fileName = Path.GetFileName(command.FileName);
        await store.SaveUpload(uploadId, content, new UploadInfo(fileName, contentType, content.Length, sha256, time.GetUtcNow()), cancellationToken);

        logger.LogInformation("Uploaded receipt {UploadId} ({ContentType}, {Size} bytes)", uploadId, contentType, content.Length);
        return Result.Success(new UploadedReceipt(uploadId, fileName, contentType, content.Length));
    }
}
