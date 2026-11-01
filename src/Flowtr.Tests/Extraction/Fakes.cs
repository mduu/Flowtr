using Flowtr.Server.Features.Extraction;
using Flowtr.Shared;

namespace Flowtr.Tests.Extraction;

/// <summary>
/// Gives the next reading that a test sets, and keeps the context that Flowtr sent.
/// </summary>
internal sealed class FakeReceiptReader : IReceiptReader
{
    public Result<ReceiptReading> Next { get; set; } = Result.Failure<ReceiptReading>("No reading set.");

    public ReadingContext? LastContext { get; private set; }

    /// <summary>
    /// The files of the last call.
    /// </summary>
    public IReadOnlyList<ReceiptContent> LastFiles { get; private set; } = [];

    public byte[]? LastContent => LastFiles.FirstOrDefault()?.Content;

    public string? LastContentType => LastFiles.FirstOrDefault()?.ContentType;

    public Task<Result<ReceiptReading>> Read(IReadOnlyList<ReceiptContent> files, ReadingContext context, CancellationToken cancellationToken)
    {
        LastContext = context;
        LastFiles = files;
        return Task.FromResult(Next);
    }
}

internal sealed class FakeExchangeRates : IExchangeRates
{
    public Dictionary<string, ExchangeRate> Rates { get; } = [];

    public Task<ExchangeRate?> ToChf(string currency, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Rates.TryGetValue(currency, out var rate) ? rate : null);
}
