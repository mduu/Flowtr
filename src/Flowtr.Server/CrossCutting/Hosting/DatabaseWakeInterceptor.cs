using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Flowtr.Server.CrossCutting.Hosting;

/// <summary>
/// Reports to <see cref="DatabaseReadiness"/> when a connection opens slowly or fails: the database wakes up.
/// </summary>
public sealed class DatabaseWakeInterceptor(DatabaseReadiness readiness) : DbConnectionInterceptor
{
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        readiness.Opening();
        return result;
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    {
        readiness.Opening();
        return ValueTask.FromResult(result);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => readiness.Opened();

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        readiness.Opened();
        return Task.CompletedTask;
    }

    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData) => readiness.Failed();

    public override Task ConnectionFailedAsync(DbConnection connection, ConnectionErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        readiness.Failed();
        return Task.CompletedTask;
    }
}
