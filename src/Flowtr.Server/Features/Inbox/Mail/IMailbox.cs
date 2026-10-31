using MimeKit;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// The mailbox that users forward receipts to. Tests use a fake: they never connect to a mail server.
/// </summary>
public interface IMailbox
{
    Task<IMailboxSession> Open(CancellationToken cancellationToken);
}

public interface IMailboxSession : IAsyncDisposable
{
    /// <summary>
    /// The IDs of the mails in the inbox folder, oldest first.
    /// </summary>
    Task<IReadOnlyList<string>> List(CancellationToken cancellationToken);

    Task<MimeMessage> Get(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a mail from the inbox folder to <paramref name="folder"/> (for example <c>Flowtr/Verarbeitet</c>). Makes the folder if it does not exist.
    /// </summary>
    Task Move(string id, string folder, CancellationToken cancellationToken);
}
