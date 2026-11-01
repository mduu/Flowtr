using Flowtr.Server.CrossCutting.Hosting;

namespace Flowtr.Tests.Hosting;

internal sealed class FakeDatabasePreparation : IDatabasePreparation
{
    public int Prepared { get; private set; }

    /// <summary>
    /// The database never answers: <see cref="Prepare"/> waits until it is cancelled.
    /// </summary>
    public bool Asleep { get; set; }

    public async Task Prepare(CancellationToken cancellationToken)
    {
        if (Asleep)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        Prepared++;
    }
}
