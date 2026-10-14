using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Flowtr.Server.Features.Extraction;

/// <summary>
/// Exchange rates to CHF, for the proposal of the CHF amount.
/// </summary>
public interface IExchangeRates
{
    /// <summary>
    /// The rate of <paramref name="currency"/> to CHF on <paramref name="date"/>, or on the last business day before.
    /// Null if no rate is known.
    /// </summary>
    Task<ExchangeRate?> ToChf(string currency, DateOnly date, CancellationToken cancellationToken);
}

/// <param name="Date">The day of the rate: on a weekend or holiday, the business day before.</param>
public sealed record ExchangeRate(decimal Rate, DateOnly Date, string Source);

/// <summary>
/// Reference rates of the European Central Bank, through the free service Frankfurter (ADR-008). Kept in memory per day.
/// </summary>
public sealed class FrankfurterExchangeRates(HttpClient http, ILogger<FrankfurterExchangeRates> logger) : IExchangeRates
{
    private static readonly ConcurrentDictionary<(string Currency, DateOnly Date), ExchangeRate?> cache = new();

    public async Task<ExchangeRate?> ToChf(string currency, DateOnly date, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue((currency, date), out var cached))
        {
            return cached;
        }

        try
        {
            var answer = await http.GetFromJsonAsync<JsonObject>(
                $"https://api.frankfurter.app/{date:yyyy-MM-dd}?from={Uri.EscapeDataString(currency)}&to=CHF", cancellationToken);

            if (answer?["rates"]?["CHF"] is JsonValue value && value.TryGetValue<decimal>(out var rate)
                && DateOnly.TryParse((string?)answer["date"], CultureInfo.InvariantCulture, out var rateDate))
            {
                return cache[(currency, date)] = new ExchangeRate(rate, rateDate, "EZB");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "No exchange rate for {Currency} on {Date}", currency, date);
        }

        return null;
    }
}
