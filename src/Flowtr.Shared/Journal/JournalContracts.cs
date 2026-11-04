namespace Flowtr.Shared.Journal;

public enum BookingKind
{
    Income,
    Expense,
}

/// <summary>
/// What the user enters for a booking. <see cref="AmountChf"/> is always positive: <see cref="Kind"/> gives the direction.
/// <see cref="Counterparty"/> and <see cref="PaymentMethod"/> are names. Flowtr adds a new name to its list.
/// <see cref="UploadIds"/> become new files of the receipt. <see cref="RemovedFileIds"/> are files of the receipt that go.
/// Without files, the booking has no receipt (story #012).
/// With <see cref="KeepUploads"/>, the uploads stay in <c>eingang</c> for a further booking of the same receipt (story #017).
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
    string? Note,
    IReadOnlyList<Guid>? UploadIds = null,
    IReadOnlyList<Guid>? RemovedFileIds = null,
    bool KeepUploads = false);

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
    IReadOnlyList<ReceiptFileView> ReceiptFiles,
    string? Note,
    bool IsLocked)
{
    public BookingInput ToInput() =>
        new(Date, Kind, AmountChf, OriginalAmount, OriginalCurrency, Description, CategoryId, Counterparty, PaymentMethod, Note);
}

/// <summary>
/// A file of a filed receipt.
/// </summary>
public sealed record ReceiptFileView(Guid Id, string FileName, string ContentType, long Size);

/// <summary>
/// <see cref="Search"/> looks in description, counterparty and note.
/// </summary>
public sealed record JournalFilter(int Year, BookingKind? Kind = null, Guid? CategoryId = null, string? Counterparty = null, string? Search = null);

/// <summary>
/// The bookings of one year for a filter, newest first.
/// <see cref="Income"/> and <see cref="Expense"/> are the totals of the whole year: the filter does not change them.
/// </summary>
public sealed record JournalView(
    int Year,
    IReadOnlyList<BookingView> Bookings,
    decimal Income,
    decimal Expense,
    IReadOnlyList<int> Years,
    bool IsClosed)
{
    /// <summary>
    /// Profit of the year, negative for a loss.
    /// </summary>
    public decimal Profit => Income - Expense;
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

/// <summary>
/// A file in <c>eingang</c>: a booking files it when it is saved.
/// </summary>
public sealed record UploadedReceipt(Guid UploadId, string FileName, string ContentType, long Size);

/// <summary>
/// The uploads that the AI reads together: the files of one receipt (story #012).
/// </summary>
public sealed record AnalysisInput(IReadOnlyList<Guid> UploadIds);

/// <summary>
/// What the AI read from a receipt (story #009). Null means: not found. The dialog fills only empty fields with it.
/// <see cref="ExchangeRateNote"/> tells how Flowtr got <see cref="AmountChf"/> from a foreign currency.
/// <see cref="MoreBookings"/> are further bookings of the same receipt, for example the fees of a payment provider (story #017).
/// </summary>
public sealed record BookingProposal(
    DateOnly? Date,
    BookingKind? Kind,
    decimal? AmountChf,
    decimal? OriginalAmount,
    string? OriginalCurrency,
    string? Description,
    Guid? CategoryId,
    string? Counterparty,
    string? PaymentMethod,
    string? ExchangeRateNote,
    string? Note = null,
    IReadOnlyList<BookingProposal>? MoreBookings = null);
