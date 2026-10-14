using Flowtr.Shared;

namespace Flowtr.Server.Features.Extraction;

/// <summary>
/// Reads a receipt with AI. The result is not checked yet: <see cref="Queries.AnalyzeUploadHandler"/> checks it.
/// </summary>
public interface IReceiptReader
{
    Task<Result<ReceiptReading>> Read(byte[] content, string contentType, ReadingContext context, CancellationToken cancellationToken);
}

/// <summary>
/// What the AI knows about Flowtr: the lists to choose from and earlier bookings per counterparty.
/// </summary>
public sealed record ReadingContext(
    DateOnly Today,
    IReadOnlyList<(Guid Id, string Name, string Kind)> Categories,
    IReadOnlyList<string> Counterparties,
    IReadOnlyList<string> PaymentMethods,
    IReadOnlyList<string> History);

/// <summary>
/// The raw answer of the AI. All values can be missing or wrong.
/// </summary>
public sealed record ReceiptReading(
    string? Date,
    string? Kind,
    decimal? Total,
    string? Currency,
    string? Description,
    string? Counterparty,
    string? CategoryId,
    string? PaymentMethod);
