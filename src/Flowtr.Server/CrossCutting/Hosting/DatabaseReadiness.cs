using Flowtr.Shared.Status;

namespace Flowtr.Server.CrossCutting.Hosting;

/// <summary>
/// Tells whether the database is ready and whether it wakes up. Azure SQL (free tier) pauses after 15 minutes
/// without a connection and needs up to a minute to resume (story #011).
/// </summary>
public sealed class DatabaseReadiness(TimeProvider time)
{
    /// <summary>
    /// A connection that takes longer to open comes from a database that wakes up.
    /// </summary>
    public static readonly TimeSpan SlowOpen = TimeSpan.FromSeconds(2);

    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource startRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Lock gate = new();
    private int opening;
    private DateTimeOffset openingSince;
    private bool failedSinceLastOpen;

    public bool IsReady => ready.Task.IsCompleted;

    public bool IsWaking
    {
        get
        {
            lock (gate)
            {
                return !IsReady || failedSinceLastOpen || (opening > 0 && time.GetUtcNow() - openingSince >= SlowOpen);
            }
        }
    }

    public StatusView Status => new(IsReady, IsWaking);

    /// <summary>
    /// A request needs the database: wake it up. Until then, it can stay paused.
    /// </summary>
    public void RequestStart() => startRequested.TrySetResult();

    public Task WaitForStartRequest(CancellationToken cancellationToken) => startRequested.Task.WaitAsync(cancellationToken);

    /// <summary>
    /// The database is connected and migrated.
    /// </summary>
    public void MarkReady()
    {
        startRequested.TrySetResult();
        ready.TrySetResult();
    }

    /// <returns>True when the database is ready, false after <paramref name="timeout"/>.</returns>
    public async Task<bool> WaitUntilReady(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            await ready.Task.WaitAsync(timeout, time, cancellationToken);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public Task WaitUntilReady(CancellationToken cancellationToken) => ready.Task.WaitAsync(cancellationToken);

    public void Opening()
    {
        lock (gate)
        {
            if (opening++ == 0)
            {
                openingSince = time.GetUtcNow();
            }
        }
    }

    public void Opened()
    {
        lock (gate)
        {
            opening = Math.Max(0, opening - 1);
            failedSinceLastOpen = false;
        }
    }

    public void Failed()
    {
        lock (gate)
        {
            opening = Math.Max(0, opening - 1);
            failedSinceLastOpen = true;
        }
    }
}
