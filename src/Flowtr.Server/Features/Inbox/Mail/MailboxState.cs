using System.Threading.Channels;

namespace Flowtr.Server.Features.Inbox.Mail;

/// <summary>
/// The result of the last fetch, for the hint in the inbox, and the lock that lets only one fetch run.
/// In memory: Flowtr runs as one instance, and the next fetch sets the state again after a restart.
/// </summary>
public sealed class MailboxState
{
    private readonly Channel<bool> wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public SemaphoreSlim Lock { get; } = new(1, 1);

    public DateTimeOffset? LastFetch { get; private set; }

    public string? LastError { get; private set; }

    public void Succeeded(DateTimeOffset at)
    {
        LastFetch = at;
        LastError = null;
    }

    public void Failed(string error) => LastError = error;

    /// <summary>
    /// Tells the <see cref="InboxWorker"/> to run now, for example to analyze new receipts.
    /// </summary>
    public void Wake() => wake.Writer.TryWrite(true);

    public async Task WaitForWake(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        try
        {
            await wake.Reader.ReadAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The interval is over.
        }
    }
}
