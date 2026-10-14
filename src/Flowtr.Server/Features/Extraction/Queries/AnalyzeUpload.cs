using System.Globalization;
using System.Text.RegularExpressions;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Server.Features.Receipts;
using Flowtr.Shared;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Extraction.Queries;

/// <summary>
/// Reads an upload with AI and makes a booking proposal (story #009). The E-mail inbox (story #010) uses it too.
/// </summary>
/// <remarks>
/// The answer of the AI is only a proposal: Flowtr keeps only valid values (date, amount, currency, a category of the
/// right kind, known payment methods), takes category and payment method of earlier bookings of the same
/// counterparty, and converts foreign currencies to CHF.
/// </remarks>
public sealed record AnalyzeUpload(Guid UploadId);

public sealed partial class AnalyzeUploadHandler(
    ApplicationDbContext db,
    IReceiptStore store,
    IReceiptReader reader,
    IExchangeRates rates,
    TimeProvider time)
    : IQueryHandler<AnalyzeUpload, BookingProposal>
{
    public async Task<Result<BookingProposal>> Handle(AnalyzeUpload query, CancellationToken cancellationToken)
    {
        if (await store.GetUpload(query.UploadId, cancellationToken) is not { } upload
            || await store.OpenUpload(query.UploadId, cancellationToken) is not { } stream)
        {
            return Result.Failure<BookingProposal>(ReceiptFiling.UploadGone);
        }

        byte[] content;
        await using (stream)
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            content = buffer.ToArray();
        }

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var reading = await reader.Read(content, upload.ContentType, await Context(today, cancellationToken), cancellationToken);
        if (!reading.IsSuccess)
        {
            return Result.Failure<BookingProposal>(reading.Error!);
        }

        return Result.Success(await Proposal(reading.Value!, today, cancellationToken));
    }

    private async Task<ReadingContext> Context(DateOnly today, CancellationToken cancellationToken)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Kind == BookingKind.Income ? 0 : 1).ThenBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name, c.Kind })
            .ToListAsync(cancellationToken);

        var history = await db.Bookings.AsNoTracking()
            .Where(b => b.Counterparty != null)
            .OrderByDescending(b => b.Date).ThenByDescending(b => b.CreatedAt)
            .Select(b => new { Counterparty = b.Counterparty!.Name, Category = b.Category!.Name, PaymentMethod = b.PaymentMethod != null ? b.PaymentMethod.Name : null })
            .Take(200)
            .ToListAsync(cancellationToken);

        return new ReadingContext(
            today,
            categories.Select(c => (c.Id, c.Name, c.Kind.ToString())).ToList(),
            await db.Counterparties.AsNoTracking().OrderBy(c => c.Name).Select(c => c.Name).ToListAsync(cancellationToken),
            await db.PaymentMethods.AsNoTracking().OrderBy(m => m.Name).Select(m => m.Name).ToListAsync(cancellationToken),
            history.DistinctBy(h => h.Counterparty).Select(h => $"{h.Counterparty}: {h.Category}, {h.PaymentMethod ?? "-"}").ToList());
    }

    private async Task<BookingProposal> Proposal(ReceiptReading reading, DateOnly today, CancellationToken cancellationToken)
    {
        var date = DateOnly.TryParseExact(reading.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                   && parsed.Year >= 2000 && parsed <= today.AddYears(1)
            ? parsed
            : (DateOnly?)null;

        var kind = Enum.TryParse<BookingKind>(reading.Kind, ignoreCase: false, out var parsedKind) ? parsedKind : (BookingKind?)null;
        var total = reading.Total is > 0 and < 1_000_000_000m ? decimal.Round(reading.Total.Value, 2) : (decimal?)null;
        var currency = reading.Currency?.ToUpperInvariant() is { } code && CurrencyCode().IsMatch(code) ? code : null;
        var description = reading.Description is { Length: > 0 } text ? text[..Math.Min(text.Length, 500)] : null;

        var counterparty = await Known(db.Counterparties.Select(c => c.Name), reading.Counterparty, cancellationToken)
                           ?? (reading.Counterparty is { Length: > 0 and <= 200 } name ? name : null);
        var paymentMethod = await Known(db.PaymentMethods.Select(m => m.Name), reading.PaymentMethod, cancellationToken);
        var categoryId = await Category(reading.CategoryId, kind, cancellationToken);

        // Earlier bookings of the same counterparty know best: the same category and payment method again.
        if (counterparty is not null && await Last(counterparty, kind, cancellationToken) is { } last)
        {
            categoryId = last.CategoryId;
            paymentMethod ??= last.PaymentMethod;
        }

        var (amountChf, original, originalCurrency, note) = await Chf(total, currency, date ?? today, cancellationToken);

        return new BookingProposal(date, kind, amountChf, original, originalCurrency, description, categoryId, counterparty, paymentMethod, note);
    }

    private async Task<(decimal? AmountChf, decimal? Original, string? Currency, string? Note)> Chf(
        decimal? total, string? currency, DateOnly date, CancellationToken cancellationToken)
    {
        if (total is null || currency is null or "CHF")
        {
            return (total, null, null, null);
        }

        if (await rates.ToChf(currency, date, cancellationToken) is not { } rate)
        {
            return (null, total, currency, $"Kein Kurs für {currency} gefunden: bitte den Betrag in CHF selbst eintragen.");
        }

        var chf = decimal.Round(total.Value * rate.Rate, 2);
        return (chf, total, currency,
            $"Umgerechnet: {currency} {SwissFormat.Amount(total.Value)} × {rate.Rate.ToString("0.0000", CultureInfo.InvariantCulture)} ({rate.Source}, {SwissFormat.Date(rate.Date)})");
    }

    private static async Task<string?> Known(IQueryable<string> names, string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var lower = name.Trim().ToLower();
        return await names.FirstOrDefaultAsync(n => n.ToLower() == lower, cancellationToken);
    }

    private async Task<Guid?> Category(string? id, BookingKind? kind, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var categoryId))
        {
            return null;
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive, cancellationToken);
        return category is not null && (kind is null || category.Kind == kind) ? category.Id : null;
    }

    private async Task<(Guid CategoryId, string? PaymentMethod)?> Last(string counterparty, BookingKind? kind, CancellationToken cancellationToken)
    {
        var last = await db.Bookings.AsNoTracking()
            .Where(b => b.Counterparty != null && b.Counterparty.Name == counterparty && (kind == null || b.Kind == kind) && b.Category!.IsActive)
            .OrderByDescending(b => b.Date).ThenByDescending(b => b.CreatedAt)
            .Select(b => new { b.CategoryId, PaymentMethod = b.PaymentMethod != null ? b.PaymentMethod.Name : null })
            .FirstOrDefaultAsync(cancellationToken);

        return last is null ? null : (last.CategoryId, last.PaymentMethod);
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCode();
}
