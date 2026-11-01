namespace Flowtr.Shared.Journal;

/// <summary>
/// Content types of receipt files besides PDF and images.
/// </summary>
public static class ReceiptContentTypes
{
    /// <summary>
    /// A table as text (CSV or TSV), for example a statement of App Store Connect (story #014).
    /// </summary>
    public const string Table = "text/csv";
}
