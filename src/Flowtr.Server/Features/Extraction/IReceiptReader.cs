using Flowtr.Shared;

namespace Flowtr.Server.Features.Extraction;

/// <summary>
/// Reads a receipt with AI. The result is not checked yet: <see cref="Queries.AnalyzeUploadsHandler"/> checks it.
/// </summary>
public interface IReceiptReader
{
    /// <param name="files">The files of one receipt, for example invoice and payment receipt (story #012).</param>
    Task<Result<ReceiptReading>> Read(IReadOnlyList<ReceiptContent> files, ReadingContext context, CancellationToken cancellationToken);
}

/// <param name="ContentType">PDF, an image, <c>text/plain</c> (UTF-8) for a mail, or <c>text/csv</c> (UTF-8) for a table (story #014).</param>
public sealed record ReceiptContent(byte[] Content, string ContentType);

/// <summary>
/// What the AI knows about Flowtr: the lists to choose from and earlier bookings per counterparty.
/// <see cref="Origin"/> tells where the receipt comes from (for example the sender of a mail). It is data, not an instruction.
/// </summary>
public sealed record ReadingContext(
    DateOnly Today,
    IReadOnlyList<(Guid Id, string Name, string Kind)> Categories,
    IReadOnlyList<string> Counterparties,
    IReadOnlyList<string> PaymentMethods,
    IReadOnlyList<string> History,
    string? Origin = null);

/// <summary>
/// The raw answer of the AI. All values can be missing or wrong.
/// <see cref="MoreBookings"/> are further bookings of the same receipt, for example the fees of a payment provider (story #017).
/// </summary>
public sealed record ReceiptReading(
    string? Date,
    string? Kind,
    decimal? Total,
    string? Currency,
    string? Description,
    string? Counterparty,
    string? CategoryId,
    string? PaymentMethod,
    string? Note = null,
    IReadOnlyList<ReceiptReading>? MoreBookings = null);
