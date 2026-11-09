using Flowtr.Server.CrossCutting.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// Story #011: the start of the app does not wake the database. A bot request can start the app.
/// </summary>
public class DatabaseStartupTests
{
    [Fact]
    public async Task Database_wakes_only_when_a_request_needs_it()
    {
        // Arrange
        var database = new FakeDatabasePreparation();
        var readiness = new DatabaseReadiness(TimeProvider.System);
        using var startup = new DatabaseStartup(database, readiness, NullLogger<DatabaseStartup>.Instance);

        // Act
        await startup.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        var preparedBefore = database.Prepared;
        readiness.RequestStart();
        var ready = await readiness.WaitUntilReady(TimeSpan.FromSeconds(5), CancellationToken.None);
        await startup.StopAsync(CancellationToken.None);

        // Assert
        (preparedBefore, database.Prepared, ready).ShouldBe((0, 1, true));
    }
}
