using Flowtr.Shared.Journal;

namespace Flowtr.Shared.Inbox;

public enum AnalysisState
{
    Pending,
    Done,
    Failed,
}

/// <summary>
/// A receipt from a mail that has no booking yet (story #010), with all files of the mail (story #012).
/// The booking dialog takes <see cref="Files"/> like files that the user uploaded.
/// <see cref="Proposal"/> is set when <see cref="Analysis"/> is done.
/// </summary>
public sealed record InboxItemView(
    Guid Id,
    IReadOnlyList<UploadedReceipt> Files,
    string Sender,
    string? OriginalSender,
    string Subject,
    DateTimeOffset ReceivedAt,
    DateTimeOffset ExpiresAt,
    AnalysisState Analysis,
    string? AnalysisError,
    BookingProposal? Proposal);

/// <summary>
/// <see cref="Address"/> is the address to forward receipts to. <see cref="LastError"/> is set when the last fetch failed.
/// </summary>
public sealed record MailboxStatus(bool IsConfigured, string? Address, DateTimeOffset? LastFetch, string? LastError);

public sealed record InboxView(IReadOnlyList<InboxItemView> Items, MailboxStatus Mailbox);

/// <summary>
/// <see cref="Receipts"/> is the number of new receipts in the inbox.
/// </summary>
public sealed record FetchResult(int Receipts, int Rejected);

public sealed record SenderView(Guid Id, string Address);

/// <summary>
/// <see cref="UserAddresses"/> are always allowed: the addresses of the users with a role.
/// </summary>
public sealed record SenderSettings(MailboxStatus Mailbox, IReadOnlyList<string> UserAddresses, IReadOnlyList<SenderView> Senders);

/// <summary>
/// An address (<c>rechnungen@example.ch</c>) or a domain (<c>@example.ch</c>).
/// </summary>
public sealed record SenderInput(string Address);

public static class MailContentTypes
{
    /// <summary>
    /// A mail without attachment as receipt: the original as <c>.eml</c> (ADR-009).
    /// </summary>
    public const string Mail = "message/rfc822";
}
