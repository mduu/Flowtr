namespace Flowtr.Server.Features.Journal.Model;

/// <summary>
/// A closed calendar year: its bookings are read-only. The year closing itself is a later story.
/// </summary>
public sealed class ClosedYear
{
    public int Year { get; set; }

    public DateTimeOffset ClosedAt { get; set; }
}
