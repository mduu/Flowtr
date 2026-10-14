using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Receipts.Model;

/// <summary>
/// A filed receipt: numbered, renamed and in the container <c>belege</c> (ADR-001). One booking has at most one receipt.
/// </summary>
public sealed class Receipt
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// For example <c>A-2026-007</c>.
    /// </summary>
    public string Number { get; set; } = "";

    public int Year { get; set; }

    public BookingKind Kind { get; set; }

    /// <summary>
    /// The number in its year and kind, from <see cref="ReceiptSequence"/>.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// For example <c>2026/ausgaben/A-2026-007_azure-abo.pdf</c>.
    /// </summary>
    public string BlobName { get; set; } = "";

    /// <summary>
    /// The name of the file that the user uploaded.
    /// </summary>
    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long Size { get; set; }

    /// <summary>
    /// SHA-256 of the content, hex. Shows that the file did not change.
    /// </summary>
    public string Sha256 { get; set; } = "";

    public DateTimeOffset FiledAt { get; set; }
}
