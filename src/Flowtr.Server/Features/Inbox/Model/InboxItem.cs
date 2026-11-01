using Flowtr.Shared.Inbox;

namespace Flowtr.Server.Features.Inbox.Model;

/// <summary>
/// A receipt from a mail that has no booking yet: one item per mail, with all its files (story #012).
/// The files are uploads in <c>eingang</c>: a booking with these uploads files them and removes the item
/// (<see cref="Receipts.ReceiptFiling"/>).
/// </summary>
public sealed class InboxItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid MailId { get; set; }

    public ImportedMail? Mail { get; set; }

    public List<InboxFile> Files { get; set; } = [];

    public DateTimeOffset ImportedAt { get; set; }

    public AnalysisState Analysis { get; set; }

    public string? AnalysisError { get; set; }

    /// <summary>
    /// The <see cref="Shared.Journal.BookingProposal"/> of the AI as JSON.
    /// </summary>
    public string? Proposal { get; set; }
}

/// <summary>
/// A file of an <see cref="InboxItem"/>: an attachment of the mail, or the mail itself.
/// </summary>
public sealed class InboxFile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid InboxItemId { get; set; }

    public int Position { get; set; }

    public Guid UploadId { get; set; }

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long Size { get; set; }

    /// <summary>
    /// SHA-256 of the file, hex. The same file comes into the inbox only once.
    /// </summary>
    public string Sha256 { get; set; } = "";
}
