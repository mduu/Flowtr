using Flowtr.Shared;
using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Export;

/// <summary>
/// The journal of a period for the export (story #013): one row per booking, oldest first, with the totals,
/// and the index of the receipt files with their SHA-256.
/// </summary>
/// <param name="Business">The name of the business (Verwaltung → Geschäft), if set.</param>
/// <param name="CreatedAt">Swiss local time.</param>
/// <param name="Status">Whether the years of the period are closed, for example «Provisorisch: 2026 ist nicht abgeschlossen.».</param>
public sealed record ExportReport(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ExportRow> Rows,
    IReadOnlyList<ReceiptIndexRow> Files,
    string? Business,
    DateTime CreatedAt,
    string CreatedBy,
    string Status)
{
    public string Title => $"Journal {SwissFormat.Date(From)} – {SwissFormat.Date(To)}";

    public string Created => $"Erstellt am {CreatedAt:dd.MM.yyyy HH:mm} von {CreatedBy} mit Flowtr";

    public int WithoutReceipt => Rows.Count(r => r.ReceiptNumber is null);

    public decimal Income => Rows.Where(r => r.Kind == BookingKind.Income).Sum(r => r.AmountChf);

    public decimal Expense => Rows.Where(r => r.Kind == BookingKind.Expense).Sum(r => r.AmountChf);

    /// <summary>
    /// Profit of the period, negative for a loss.
    /// </summary>
    public decimal Profit => Income - Expense;

    /// <summary>
    /// The totals per category: income first, then expenses, each by name.
    /// </summary>
    public IReadOnlyList<CategoryTotal> Categories =>
        Rows.GroupBy(r => (r.Kind, r.Category))
            .Select(group => new CategoryTotal(group.Key.Category, group.Key.Kind, group.Sum(r => r.AmountChf), group.Count()))
            .OrderBy(total => total.Kind == BookingKind.Income ? 0 : 1).ThenBy(total => total.Category, StringComparer.CurrentCulture)
            .ToList();
}

public sealed record ExportRow(
    DateOnly Date,
    string? ReceiptNumber,
    BookingKind Kind,
    string Category,
    string Description,
    string? Counterparty,
    string? PaymentMethod,
    decimal AmountChf,
    decimal? OriginalAmount,
    string? OriginalCurrency,
    string? Note,
    int Files)
{
    public string KindName => Kind == BookingKind.Income ? "Einnahme" : "Ausgabe";
}

public sealed record CategoryTotal(string Category, BookingKind Kind, decimal AmountChf, int Bookings)
{
    public string KindName => Kind == BookingKind.Income ? "Einnahmen" : "Ausgaben";
}

/// <summary>
/// A mail as receipt, to print as PDF (ADR-009).
/// </summary>
public sealed record MailPrint(string? From, string? To, DateTimeOffset? Date, string Subject, string Body);

/// <summary>
/// A file of a receipt in the index (Belegverzeichnis): with its SHA-256 the file can be shown to be unchanged (GeBüV).
/// </summary>
/// <param name="Path">The path in the ZIP. Null when the export is without receipts.</param>
/// <param name="BlobName">Where Flowtr keeps the file (container <c>belege</c>).</param>
public sealed record ReceiptIndexRow(
    string BlobName,
    string Number,
    DateOnly Date,
    BookingKind Kind,
    string Description,
    string? Path,
    string FileName,
    string ContentType,
    long Size,
    string Sha256)
{
    public string KindName => Kind == BookingKind.Income ? "Einnahme" : "Ausgabe";
}
