namespace Flowtr.Server.Features.Inbox.Model;

/// <summary>
/// A mail that Flowtr read from the mailbox, once per <see cref="MessageKey"/>. It stops a second import of the same mail (ADR-009).
/// </summary>
public sealed class ImportedMail
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// SHA-256 (hex) of the <c>Message-ID</c>, or of sender, date and subject when the mail has no <c>Message-ID</c>.
    /// </summary>
    public string MessageKey { get; set; } = "";

    public string? MessageId { get; set; }

    public string Sender { get; set; } = "";

    /// <summary>
    /// The sender of a forwarded mail, if Flowtr finds it (for example <c>Microsoft &lt;billing@microsoft.com&gt;</c>).
    /// </summary>
    public string? OriginalSender { get; set; }

    public string Subject { get; set; } = "";

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset ImportedAt { get; set; }

    public MailOutcome Outcome { get; set; }

    /// <summary>
    /// Why Flowtr rejected the mail.
    /// </summary>
    public string? Reason { get; set; }
}

public enum MailOutcome
{
    /// <summary>
    /// At least one receipt is in the inbox.
    /// </summary>
    Imported,

    /// <summary>
    /// All receipts of the mail were already known.
    /// </summary>
    Duplicate,

    /// <summary>
    /// The sender is not allowed or not confirmed.
    /// </summary>
    Rejected,
}
