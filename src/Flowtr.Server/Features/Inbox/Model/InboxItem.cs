using Flowtr.Shared.Inbox;

namespace Flowtr.Server.Features.Inbox.Model;

/// <summary>
/// A receipt from a mail that has no booking yet. Its file is an upload in <c>eingang</c>:
/// a booking with this upload files it and removes the item (<see cref="Receipts.ReceiptFiling"/>).
/// </summary>
public sealed class InboxItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid MailId { get; set; }

    public ImportedMail? Mail { get; set; }

    public Guid UploadId { get; set; }

    public string FileName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long Size { get; set; }

    /// <summary>
    /// SHA-256 of the file, hex. The same file comes into the inbox only once.
    /// </summary>
    public string Sha256 { get; set; } = "";

    public DateTimeOffset ImportedAt { get; set; }

    public AnalysisState Analysis { get; set; }

    public string? AnalysisError { get; set; }

    /// <summary>
    /// The <see cref="Shared.Journal.BookingProposal"/> of the AI as JSON.
    /// </summary>
    public string? Proposal { get; set; }
}
