namespace Flowtr.Shared.Journal;

public enum BookingKind
{
    Income,
    Expense,
}

/// <summary>
/// What the user enters for a booking. <see cref="AmountChf"/> is always positive: <see cref="Kind"/> gives the direction.
/// <see cref="Counterparty"/> and <see cref="PaymentMethod"/> are names. Flowtr adds a new name to its list.
/// </summary>
public sealed record BookingInput(
    DateOnly Date,
    BookingKind Kind,
    decimal AmountChf,
    decimal? OriginalAmount,
    string? OriginalCurrency,
    string Description,
    Guid CategoryId,
    string? Counterparty,
    string? PaymentMethod,
    string? Note);

/// <summary>
/// A booking in the journal. <see cref="IsLocked"/> is true when its year is closed.
/// </summary>
public sealed record BookingView(
    Guid Id,
    DateOnly Date,
    BookingKind Kind,
    decimal AmountChf,
    decimal? OriginalAmount,
    string? OriginalCurrency,
    string Description,
    Guid CategoryId,
    string Category,
    string? Counterparty,
    string? PaymentMethod,
    string? ReceiptNumber,
    string? Note,
    bool IsLocked)
{
    public BookingInput ToInput() =>
        new(Date, Kind, AmountChf, OriginalAmount, OriginalCurrency, Description, CategoryId, Counterparty, PaymentMethod, Note);
}

/// <summary>
/// <see cref="Search"/> looks in description, counterparty and note.
/// </summary>
public sealed record JournalFilter(int Year, BookingKind? Kind = null, Guid? CategoryId = null, string? Counterparty = null, string? Search = null);

/// <summary>
/// The bookings of one year for a filter, newest first, with totals of the shown bookings.
/// </summary>
public sealed record JournalView(
    int Year,
    IReadOnlyList<BookingView> Bookings,
    decimal Income,
    decimal Expense,
    IReadOnlyList<int> Years,
    bool IsClosed)
{
    public decimal Result => Income - Expense;
}

/// <summary>
/// <see cref="Usage"/> is the number of bookings with this category.
/// </summary>
public sealed record CategoryView(Guid Id, string Name, BookingKind Kind, bool IsActive, int Usage);

public sealed record CategoryInput(string Name, BookingKind Kind);

public sealed record CategoryUpdate(string Name, bool IsActive);

/// <summary>
/// Lists for the booking form. <see cref="Categories"/> also contains inactive categories: an old booking can still use one.
/// </summary>
public sealed record BookingOptions(IReadOnlyList<CategoryView> Categories, IReadOnlyList<string> Counterparties, IReadOnlyList<string> PaymentMethods);
