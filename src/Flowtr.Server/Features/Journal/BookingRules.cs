using System.Text.RegularExpressions;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Journal;

/// <summary>
/// Checks and applies a <see cref="BookingInput"/>. Create and update use the same rules.
/// </summary>
public sealed partial class BookingRules(ApplicationDbContext db, TimeProvider time)
{
    public const string YearClosed = "Das Jahr {0} ist abgeschlossen. Seine Buchungen lassen sich nicht mehr ändern.";

    private const decimal MaxAmount = 1_000_000_000m;

    /// <summary>
    /// Gives a German message for the user, or null if the input is valid.
    /// </summary>
    /// <param name="currentCategoryId">The category of the booking before the change: it may be inactive.</param>
    public async Task<string?> Check(BookingInput input, Guid? currentCategoryId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

        if (input.Date.Year < 2000 || input.Date > today.AddYears(1))
        {
            return "Bitte ein gültiges Datum angeben.";
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            return "Bitte eine Beschreibung angeben.";
        }

        if (input.AmountChf <= 0 || input.AmountChf >= MaxAmount)
        {
            return "Der Betrag in CHF muss grösser als 0 sein.";
        }

        if (!HasCents(input.AmountChf) || input.OriginalAmount is { } original && !HasCents(original))
        {
            return "Beträge haben höchstens zwei Nachkommastellen.";
        }

        if (CheckOriginal(input) is { } originalError)
        {
            return originalError;
        }

        if (TooLong(input) is { } lengthError)
        {
            return lengthError;
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == input.CategoryId, cancellationToken);
        if (category is null || category.Kind != input.Kind || (!category.IsActive && category.Id != currentCategoryId))
        {
            return "Bitte eine passende Kategorie wählen.";
        }

        return await IsClosed(input.Date.Year, cancellationToken) ? string.Format(YearClosed, input.Date.Year) : null;
    }

    public Task<bool> IsClosed(int year, CancellationToken cancellationToken) =>
        db.ClosedYears.AnyAsync(closed => closed.Year == year, cancellationToken);

    /// <summary>
    /// Copies the input to <paramref name="booking"/>. New counterparties and payment methods go into their lists.
    /// </summary>
    public async Task Apply(Booking booking, BookingInput input, CancellationToken cancellationToken)
    {
        booking.Date = input.Date;
        booking.Kind = input.Kind;
        booking.AmountChf = input.AmountChf;
        booking.OriginalAmount = input.OriginalAmount;
        booking.OriginalCurrency = Trimmed(input.OriginalCurrency)?.ToUpperInvariant();
        booking.Description = input.Description.Trim();
        booking.CategoryId = input.CategoryId;
        booking.CounterpartyId = await CounterpartyId(Trimmed(input.Counterparty), cancellationToken);
        booking.PaymentMethodId = await PaymentMethodId(Trimmed(input.PaymentMethod), cancellationToken);
        booking.Note = Trimmed(input.Note);
    }

    private static string? CheckOriginal(BookingInput input)
    {
        var currency = Trimmed(input.OriginalCurrency);

        if (currency is null && input.OriginalAmount is null)
        {
            return null;
        }

        if (currency is null || input.OriginalAmount is null)
        {
            return "Für den Originalbetrag braucht es Betrag und Währung.";
        }

        if (!CurrencyCode().IsMatch(currency.ToUpperInvariant()))
        {
            return "Die Währung braucht drei Buchstaben, zum Beispiel USD.";
        }

        if (currency.Equals("CHF", StringComparison.OrdinalIgnoreCase))
        {
            return "Der Originalbetrag ist nur für Fremdwährungen. Bei CHF bleibt er leer.";
        }

        return input.OriginalAmount <= 0 || input.OriginalAmount >= MaxAmount ? "Der Originalbetrag muss grösser als 0 sein." : null;
    }

    private static string? TooLong(BookingInput input) =>
        input.Description.Trim().Length > 500 ? "Die Beschreibung ist zu lang (höchstens 500 Zeichen)."
        : input.Note?.Trim().Length > 2000 ? "Die Notiz ist zu lang (höchstens 2000 Zeichen)."
        : input.Counterparty?.Trim().Length > 200 ? "Der Name der Gegenpartei ist zu lang (höchstens 200 Zeichen)."
        : input.PaymentMethod?.Trim().Length > 100 ? "Die Zahlungsart ist zu lang (höchstens 100 Zeichen)."
        : null;

    private async Task<Guid?> CounterpartyId(string? name, CancellationToken cancellationToken)
    {
        if (name is null)
        {
            return null;
        }

        var existing = await db.Counterparties.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower(), cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var counterparty = new Counterparty { Name = name };
        db.Counterparties.Add(counterparty);
        return counterparty.Id;
    }

    private async Task<Guid?> PaymentMethodId(string? name, CancellationToken cancellationToken)
    {
        if (name is null)
        {
            return null;
        }

        var existing = await db.PaymentMethods.FirstOrDefaultAsync(m => m.Name.ToLower() == name.ToLower(), cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var method = new PaymentMethod { Name = name };
        db.PaymentMethods.Add(method);
        return method.Id;
    }

    private static bool HasCents(decimal amount) => decimal.Round(amount, 2) == amount;

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();
}
