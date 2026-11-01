using Flowtr.Server.CrossCutting.Hosting;

namespace Flowtr.Tests.Hosting;

/// <summary>
/// Story #011: Flowtr knows when the database wakes up from a pause.
/// </summary>
public class DatabaseReadinessTests
{
    private readonly TestClock clock = new();

    [Fact]
    public void Database_wakes_until_it_is_ready()
    {
        var readiness = new DatabaseReadiness(clock);

        readiness.Status.ShouldBe(new(Ready: false, Waking: true));
        readiness.MarkReady();
        readiness.Status.ShouldBe(new(Ready: true, Waking: false));
    }

    [Fact]
    public void Slow_connection_means_the_database_wakes()
    {
        // Arrange
        var readiness = Ready();

        // Act
        readiness.Opening();
        var quick = readiness.IsWaking;
        clock.Now += DatabaseReadiness.SlowOpen;
        var slow = readiness.IsWaking;
        readiness.Opened();

        // Assert
        (quick, slow, readiness.IsWaking).ShouldBe((false, true, false));
    }

    [Fact]
    public void Failed_connection_means_the_database_wakes_until_a_connection_opens()
    {
        // Arrange
        var readiness = Ready();

        // Act
        readiness.Opening();
        readiness.Failed();
        var afterFailure = readiness.IsWaking;
        readiness.Opening();
        readiness.Opened();

        // Assert
        (afterFailure, readiness.IsWaking).ShouldBe((true, false));
    }

    [Fact]
    public async Task Wait_ends_when_the_database_is_ready()
    {
        var readiness = new DatabaseReadiness(TimeProvider.System);

        var waiting = readiness.WaitUntilReady(TimeSpan.FromSeconds(10), CancellationToken.None);
        readiness.MarkReady();

        (await waiting).ShouldBeTrue();
    }

    [Fact]
    public async Task Wait_ends_after_the_timeout()
    {
        var readiness = new DatabaseReadiness(TimeProvider.System);

        (await readiness.WaitUntilReady(TimeSpan.FromMilliseconds(20), CancellationToken.None)).ShouldBeFalse();
    }

    private DatabaseReadiness Ready()
    {
        var readiness = new DatabaseReadiness(clock);
        readiness.MarkReady();
        return readiness;
    }
}
