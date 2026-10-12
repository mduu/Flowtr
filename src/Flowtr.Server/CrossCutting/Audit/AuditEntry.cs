namespace Flowtr.Server.CrossCutting.Audit;

/// <summary>
/// One change of an audited entity. Flowtr only adds entries. A database trigger stops changes and deletes.
/// </summary>
public sealed class AuditEntry
{
    public long Id { get; set; }

    public DateTimeOffset At { get; set; }

    /// <summary>
    /// Null for changes without a signed-in user (see <see cref="AuditActor"/>).
    /// </summary>
    public string? UserId { get; set; }

    public string UserName { get; set; } = "";

    public AuditAction Action { get; set; }

    /// <summary>
    /// The key of the <see cref="AuditRule"/>, for example <c>User</c>.
    /// </summary>
    public string EntityType { get; set; } = "";

    /// <summary>
    /// The primary key. Parts of a composite key are separated by <c>/</c>.
    /// </summary>
    public string EntityId { get; set; } = "";

    /// <summary>
    /// JSON object: property name → <c>{ "Old": …, "New": … }</c>. Hidden values are <c>***</c>.
    /// </summary>
    public string Changes { get; set; } = "{}";
}

public enum AuditAction
{
    Created,
    Modified,
    Deleted,
}
