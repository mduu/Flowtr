using Flowtr.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.CrossCutting.Hosting;

/// <summary>
/// Connects to the database and applies the migrations. Tests use a fake: EF Core in memory has no migrations.
/// </summary>
public interface IDatabasePreparation
{
    /// <summary>
    /// Waits until the database answers (it can wake up from a pause), then migrates it.
    /// </summary>
    Task Prepare(CancellationToken cancellationToken);
}

public sealed class DatabasePreparation(IServiceScopeFactory scopes, ILogger<DatabasePreparation> logger) : IDatabasePreparation
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public async Task Prepare(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        while (!await db.Database.CanConnectAsync(cancellationToken))
        {
            logger.LogInformation("Database not ready yet, it can wake up from a pause");
            await Task.Delay(RetryDelay, cancellationToken);
        }

        // EF Core locks the migration: the app and the inbox job can start at the same time.
        await db.Database.MigrateAsync(cancellationToken);
    }
}

/// <summary>
/// Prepares the database when the first request needs it, then marks it ready. Until then, <see cref="StartupGate"/>
/// shows «Flowtr startet …».
/// </summary>
/// <remarks>
/// Not at the start of the app: a bot request starts the app, and the database must stay paused (free tier).
/// </remarks>
public sealed class DatabaseStartup(IDatabasePreparation database, DatabaseReadiness readiness, ILogger<DatabaseStartup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await readiness.WaitForStartRequest(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await database.Prepare(stoppingToken);
                readiness.MarkReady();
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Database not prepared, Flowtr tries again");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
