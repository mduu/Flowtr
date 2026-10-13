using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Journal.Model;

/// <summary>
/// One line in the journal (see specs/journal.md).
/// </summary>
public sealed class Booking
{
    /// <summary>
    /// Set before the save, thus the audit log knows it (ADR-006).
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// When the booking was entered. It sorts bookings of the same date.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Date on the receipt. Its year is the tax period.
    /// </summary>
    public DateOnly Date { get; set; }

    public BookingKind Kind { get; set; }

    /// <summary>
    /// Always positive. <see cref="Kind"/> gives the direction.
    /// </summary>
    public decimal AmountChf { get; set; }

    public decimal? OriginalAmount { get; set; }

    /// <summary>
    /// ISO 4217 code, for example <c>USD</c>.
    /// </summary>
    public string? OriginalCurrency { get; set; }

    public string Description { get; set; } = "";

    public Guid CategoryId { get; set; }

    public Category? Category { get; set; }

    public Guid? CounterpartyId { get; set; }

    public Counterparty? Counterparty { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// For example <c>A-2026-001</c>. Story #008 gives it.
    /// </summary>
    public string? ReceiptNumber { get; set; }

    public string? Note { get; set; }

    /// <summary>
    /// Not used yet: Flowtr is not subject to VAT. The model is ready for it.
    /// </summary>
    public decimal? VatRate { get; set; }

    /// <inheritdoc cref="VatRate"/>
    public decimal? VatAmount { get; set; }
}
