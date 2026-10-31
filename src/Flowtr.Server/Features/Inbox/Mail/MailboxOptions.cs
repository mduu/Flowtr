using MailKit.Security;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// Configuration section <c>Mailbox</c>. The password only in user secrets or Key Vault, never in the repository.
/// </summary>
public sealed class MailboxOptions
{
    public const string Section = "Mailbox";

    public string? Host { get; set; }

    public int Port { get; set; } = 993;

    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.SslOnConnect;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    /// <summary>
    /// The address that users forward receipts to. Default: <see cref="UserName"/>.
    /// </summary>
    public string? Address { get; set; }

    public string Inbox { get; set; } = "INBOX";

    public string ProcessedFolder { get; set; } = "Flowtr/Verarbeitet";

    public string RejectedFolder { get; set; } = "Flowtr/Abgelehnt";

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);

    public string? ShownAddress => string.IsNullOrWhiteSpace(Address) ? UserName : Address;
}
