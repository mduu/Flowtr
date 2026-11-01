namespace Flowtr.Shared.Status;

/// <summary>
/// The answer of <c>GET /status</c>. Flowtr answers it without the database.
/// </summary>
/// <param name="Ready">The database is connected and migrated since the start.</param>
/// <param name="Waking">The database does not answer yet, for example because it wakes up from a pause.</param>
public sealed record StatusView(bool Ready, bool Waking)
{
    public const string Path = "status";
}
