using Flowtr.Shared.Journal;

namespace Flowtr.Server.Features.Receipts.Model;

/// <summary>
/// A filed receipt: one number, one or more files in the container <c>belege</c> (ADR-001, story #012).
/// One booking has at most one receipt.
/// </summary>
public sealed class Receipt
{
    /// <summary>
    /// A receipt has at most this number of files.
    /// </summary>
    public const int MaxFiles = 10;

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
    /// The position of the last file that was added, also if it was removed later: positions are never used again.
    /// </summary>
    public int LastFilePosition { get; set; }

    public List<ReceiptFile> Files { get; set; } = [];
}

/// <summary>
/// A file of a <see cref="Receipt"/>, unchanged as the user uploaded it or as it came with a mail.
/// </summary>
public sealed class ReceiptFile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ReceiptId { get; set; }

    /// <summary>
    /// 1 for the first file. A position is never used again: the blob names stay unique.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// For example <c>2026/ausgaben/A-2026-007_azure-abo.pdf</c>, from the second file on <c>…_azure-abo_2.pdf</c>.
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
