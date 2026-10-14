using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Server.Features.Receipts.Model;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Receipts;

/// <summary>
/// Files the receipt of a booking: number, blob in <c>belege</c>, <see cref="Receipt"/> row (ADR-001, ADR-007).
/// Order: first the blob, then the database. On a failure, the blob is deleted again.
/// </summary>
public sealed class ReceiptFiling(ApplicationDbContext db, IReceiptStore store, TimeProvider time, ILogger<ReceiptFiling> logger)
{
    public const string UploadGone = "Der hochgeladene Beleg ist nicht mehr da. Bitte lade ihn nochmals hoch.";

    private const int Attempts = 5;

    /// <summary>
    /// Saves <paramref name="booking"/> (already tracked) together with the upload as its receipt.
    /// A new receipt gets the next number of its year and kind. A replaced receipt keeps its number.
    /// </summary>
    public async Task<Result> SaveWithReceipt(Booking booking, Guid uploadId, CancellationToken cancellationToken)
    {
        if (await store.GetUpload(uploadId, cancellationToken) is not { } upload)
        {
            return Result.Failure(UploadGone);
        }

        var existing = booking.ReceiptId is { } id ? await db.Set<Receipt>().FindAsync([id], cancellationToken) : null;
        var oldBlob = existing?.BlobName;

        for (var attempt = 1; ; attempt++)
        {
            var (receipt, sequence) = existing is null ? await NewReceipt(booking, cancellationToken) : (existing, null);
            receipt.BlobName = ReceiptFiles.BlobName(booking.Date.Year, booking.Kind == BookingKind.Income, receipt.Number, booking.Description, Extension(upload.ContentType));
            receipt.FileName = upload.FileName;
            receipt.ContentType = upload.ContentType;
            receipt.Size = upload.Size;
            receipt.Sha256 = upload.Sha256;
            receipt.FiledAt = time.GetUtcNow();

            // Never overwrite the file of another receipt: a taken name means a taken number.
            var replacesOwnFile = receipt.BlobName == oldBlob;
            if (!await store.CopyToReceipts(uploadId, receipt.BlobName, upload.ContentType, replacesOwnFile, cancellationToken))
            {
                if (attempt == Attempts || sequence is null)
                {
                    return Result.Failure($"Der Beleg {receipt.Number} existiert schon. Bitte versuche es nochmals.");
                }

                logger.LogInformation("Receipt file {BlobName} exists, attempt {Attempt}", receipt.BlobName, attempt);
                await Reset(receipt, sequence, cancellationToken);
                continue;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.Entries.All(entry => entry.Entity is ReceiptSequence))
            {
                // Another save took the number at the same time: try again with the next one.
                logger.LogInformation("Receipt number {Number} taken in parallel, attempt {Attempt}", receipt.Number, attempt);
                await store.DeleteReceipt(receipt.BlobName, cancellationToken);
                await Reset(receipt, sequence, cancellationToken);
                continue;
            }
            catch when (!replacesOwnFile)
            {
                await store.DeleteReceipt(receipt.BlobName, cancellationToken);
                throw;
            }

            await store.DeleteUpload(uploadId, cancellationToken);
            if (oldBlob is not null && oldBlob != receipt.BlobName)
            {
                await store.DeleteReceipt(oldBlob, cancellationToken);
            }

            logger.LogInformation("Filed receipt {Number} as {BlobName}", receipt.Number, receipt.BlobName);
            return Result.Success();
        }
    }

    /// <summary>
    /// Removes the receipt of <paramref name="booking"/> from the database. Gives the blob to delete after the save.
    /// </summary>
    public async Task<string?> Detach(Booking booking, CancellationToken cancellationToken)
    {
        if (booking.ReceiptId is not { } id || await db.Set<Receipt>().FindAsync([id], cancellationToken) is not { } receipt)
        {
            return null;
        }

        booking.ReceiptId = null;
        db.Set<Receipt>().Remove(receipt);
        return receipt.BlobName;
    }

    /// <summary>
    /// Deletes a blob after the database save. A failure only goes to the log: the housekeeping job finds the blob later.
    /// </summary>
    public async Task DeleteAfterSave(string? blobName, CancellationToken cancellationToken)
    {
        if (blobName is null)
        {
            return;
        }

        try
        {
            await store.DeleteReceipt(blobName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Receipt blob {BlobName} not deleted", blobName);
        }
    }

    /// <summary>
    /// The year and kind of a booking with a receipt must not change: they are part of the receipt number.
    /// </summary>
    public static string? CheckChange(Booking booking, BookingInput input) =>
        booking.ReceiptId is not null && input.UploadId is null && !input.RemoveReceipt
        && (booking.Date.Year != input.Date.Year || booking.Kind != input.Kind)
            ? "Jahr und Art einer Buchung mit Beleg lassen sich nicht ändern: Sie sind Teil der Belegnummer. Entferne zuerst den Beleg."
            : null;

    public static string Number(BookingKind kind, int year, int sequenceNumber) =>
        $"{(kind == BookingKind.Income ? "E" : "A")}-{year}-{sequenceNumber:000}";

    private async Task<(Receipt Receipt, ReceiptSequence Sequence)> NewReceipt(Booking booking, CancellationToken cancellationToken)
    {
        var year = booking.Date.Year;
        var sequence = await db.Set<ReceiptSequence>().FindAsync([year, booking.Kind], cancellationToken);
        if (sequence is null)
        {
            sequence = new ReceiptSequence { Year = year, Kind = booking.Kind };
            db.Set<ReceiptSequence>().Add(sequence);
        }

        sequence.LastNumber++;

        var receipt = new Receipt
        {
            Year = year,
            Kind = booking.Kind,
            SequenceNumber = sequence.LastNumber,
            Number = Number(booking.Kind, year, sequence.LastNumber),
        };
        db.Set<Receipt>().Add(receipt);
        booking.ReceiptId = receipt.Id;

        return (receipt, sequence);
    }

    private async Task Reset(Receipt receipt, ReceiptSequence? sequence, CancellationToken cancellationToken)
    {
        db.Entry(receipt).State = EntityState.Detached;

        if (sequence is not null)
        {
            var entry = db.Entry(sequence);
            if (entry.State == EntityState.Added)
            {
                entry.State = EntityState.Detached;
            }
            else
            {
                await entry.ReloadAsync(cancellationToken);
            }
        }
    }

    private static string Extension(string contentType) => contentType switch
    {
        "application/pdf" => ".pdf",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        _ => "",
    };
}
