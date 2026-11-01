namespace Flowtr.Client.Features.Status;

/// <summary>
/// Tells the UI that the database wakes up, while a call to the server waits for it (story #011).
/// </summary>
public sealed class ServerStatus
{
    private readonly Lock gate = new();
    private int waking;

    public bool IsWaking
    {
        get
        {
            lock (gate)
            {
                return waking > 0;
            }
        }
    }

    public event Action? Changed;

    /// <summary>
    /// A call waits for the database. Dispose the result when the call is done.
    /// </summary>
    public IDisposable Waking()
    {
        Change(+1);
        return new Release(() => Change(-1));
    }

    private void Change(int delta)
    {
        bool before, after;
        lock (gate)
        {
            before = waking > 0;
            waking += delta;
            after = waking > 0;
        }

        if (before != after)
        {
            Changed?.Invoke();
        }
    }

    private sealed class Release(Action release) : IDisposable
    {
        private Action? pending = release;

        public void Dispose() => Interlocked.Exchange(ref pending, null)?.Invoke();
    }
}
