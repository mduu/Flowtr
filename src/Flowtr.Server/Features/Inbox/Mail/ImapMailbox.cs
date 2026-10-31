using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// <see cref="IMailbox"/> with IMAP (MailKit).
/// </summary>
public sealed class ImapMailbox(IOptions<MailboxOptions> options) : IMailbox
{
    public async Task<IMailboxSession> Open(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("The mailbox is not configured.");
        }

        var client = new ImapClient { Timeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds };
        try
        {
            await client.ConnectAsync(settings.Host!, settings.Port, settings.Security, cancellationToken);
            await client.AuthenticateAsync(settings.UserName!, settings.Password!, cancellationToken);
            var inbox = await client.GetFolderAsync(settings.Inbox, cancellationToken);
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            return new Session(client, inbox);
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private sealed class Session(ImapClient client, IMailFolder inbox) : IMailboxSession
    {
        private readonly Dictionary<string, IMailFolder> folders = [];

        public async Task<IReadOnlyList<string>> List(CancellationToken cancellationToken) =>
            (await inbox.SearchAsync(SearchQuery.All, cancellationToken)).Select(id => id.ToString()).ToList();

        public Task<MimeMessage> Get(string id, CancellationToken cancellationToken) =>
            inbox.GetMessageAsync(UniqueId.Parse(id), cancellationToken);

        public async Task Move(string id, string folder, CancellationToken cancellationToken) =>
            await inbox.MoveToAsync(UniqueId.Parse(id), await Folder(folder, cancellationToken), cancellationToken);

        public async ValueTask DisposeAsync()
        {
            try
            {
                await client.DisconnectAsync(true);
            }
            catch (Exception exception) when (exception is IOException or ImapProtocolException or ServiceNotConnectedException)
            {
                // The connection is gone already.
            }

            client.Dispose();
        }

        private async Task<IMailFolder> Folder(string path, CancellationToken cancellationToken)
        {
            if (folders.TryGetValue(path, out var known))
            {
                return known;
            }

            var folder = client.GetFolder(client.PersonalNamespaces[0])
                ?? throw new InvalidOperationException("The mailbox has no personal namespace.");
            foreach (var name in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    folder = await folder.GetSubfolderAsync(name, cancellationToken);
                }
                catch (FolderNotFoundException)
                {
                    folder = await folder.CreateAsync(name, isMessageFolder: true, cancellationToken)
                        ?? throw new InvalidOperationException($"Folder {name} not created.");
                }
            }

            folders[path] = folder;
            return folder;
        }
    }
}
