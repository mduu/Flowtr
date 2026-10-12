namespace Flowtr.Shared.Audit;

/// <summary>
/// Filter for the audit log. <see cref="From"/> and <see cref="To"/> are inclusive, <see cref="User"/> is part of the user name.
/// </summary>
public sealed record AuditFilter(DateTimeOffset? From, DateTimeOffset? To, string? User, string? Area, int Skip = 0, int Take = 50);

/// <summary>
/// One entry, ready to show: texts in German, values formatted.
/// </summary>
public sealed record AuditEntryView(
    long Id,
    DateTimeOffset At,
    string UserName,
    string Action,
    string Area,
    string Subject,
    IReadOnlyList<AuditChangeView> Changes);

/// <summary>
/// A changed property. Null values show as empty. Hidden values (for example a password) show as <c>verborgen</c>.
/// </summary>
public sealed record AuditChangeView(string Property, string? Old, string? New);

public sealed record AuditPage(IReadOnlyList<AuditEntryView> Entries, int Total);

/// <summary>
/// Areas of the audit log, as stored (key) and as shown (label).
/// </summary>
public static class AuditAreas
{
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["User"] = "Benutzer",
        ["UserRole"] = "Rolle",
        ["Passkey"] = "Passkey",
    };
}
