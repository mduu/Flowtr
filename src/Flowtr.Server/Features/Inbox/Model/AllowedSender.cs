namespace Flowtr.Server.Features.Inbox.Model;

/// <summary>
/// A sender whose mails Flowtr takes into the inbox, in addition to the users. <see cref="Address"/> is lower case:
/// an address (<c>rechnungen@example.ch</c>) or a domain (<c>@example.ch</c>).
/// </summary>
public sealed class AllowedSender
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Address { get; set; } = "";
}
