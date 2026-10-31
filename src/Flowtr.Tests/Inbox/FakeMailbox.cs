using Flowtr.Server.Features.Inbox.Mail;
using MimeKit;

namespace Flowtr.Tests.Inbox;

/// <summary>
/// <see cref="IMailbox"/> in memory: the inbox folder and the folders that mails were moved to.
/// </summary>
internal sealed class FakeMailbox : IMailbox
{
    private int nextId;

    public List<(string Id, MimeMessage Message)> Inbox { get; } = [];

    public Dictionary<string, List<MimeMessage>> Folders { get; } = [];

    /// <summary>
    /// Thrown by <see cref="Open"/>, for example to simulate a mail server that is down.
    /// </summary>
    public Exception? OpenError { get; set; }

    /// <summary>
    /// Thrown once by the next move.
    /// </summary>
    public Exception? MoveError { get; set; }

    public void Receive(MimeMessage message) => Inbox.Add(((++nextId).ToString(), message));

    public IReadOnlyList<MimeMessage> Folder(string name) => Folders.GetValueOrDefault(name) ?? [];

    public Task<IMailboxSession> Open(CancellationToken cancellationToken) =>
        OpenError is { } error ? Task.FromException<IMailboxSession>(error) : Task.FromResult<IMailboxSession>(new Session(this));

    private sealed class Session(FakeMailbox mailbox) : IMailboxSession
    {
        public Task<IReadOnlyList<string>> List(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(mailbox.Inbox.Select(mail => mail.Id).ToList());

        public Task<MimeMessage> Get(string id, CancellationToken cancellationToken) =>
            Task.FromResult(mailbox.Inbox.Single(mail => mail.Id == id).Message);

        public Task Move(string id, string folder, CancellationToken cancellationToken)
        {
            if (mailbox.MoveError is { } error)
            {
                mailbox.MoveError = null;
                throw error;
            }

            var mail = mailbox.Inbox.Single(m => m.Id == id);
            mailbox.Inbox.Remove(mail);
            if (!mailbox.Folders.TryGetValue(folder, out var messages))
            {
                mailbox.Folders[folder] = messages = [];
            }

            messages.Add(mail.Message);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
