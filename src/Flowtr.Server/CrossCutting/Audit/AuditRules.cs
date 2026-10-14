using Flowtr.Server.Data;
using Flowtr.Server.Features.Journal.Model;
using Flowtr.Server.Features.Receipts.Model;
using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.CrossCutting.Audit;

/// <summary>
/// Which entities Flowtr audits, and how.
/// </summary>
/// <param name="Key">Stable name in the audit log. Do not change it after the first entry.</param>
/// <param name="Hidden">Properties that only show that they changed (value <c>***</c>), for example password hashes.</param>
/// <param name="Ignored">Properties that the audit log does not record. A change of only these properties gives no entry.</param>
/// <param name="AuditModified">False to record only create and delete.</param>
public sealed record AuditRule(string Key, IReadOnlySet<string> Hidden, IReadOnlySet<string> Ignored, bool AuditModified = true);

public static class AuditRules
{
    public const string HiddenValue = "***";

    private static readonly Dictionary<Type, AuditRule> rules = new()
    {
        [typeof(ApplicationUser)] = new AuditRule(
            "User",
            Hidden: new HashSet<string> { nameof(ApplicationUser.PasswordHash), nameof(ApplicationUser.SecurityStamp) },
            Ignored: new HashSet<string>
            {
                nameof(ApplicationUser.ConcurrencyStamp),
                nameof(ApplicationUser.NormalizedEmail),
                nameof(ApplicationUser.NormalizedUserName),
            }),
        [typeof(IdentityUserRole<string>)] = new AuditRule("UserRole", Hidden: new HashSet<string>(), Ignored: new HashSet<string>()),

        [typeof(Booking)] = new AuditRule("Booking", Hidden: new HashSet<string>(), Ignored: new HashSet<string> { nameof(Booking.CreatedAt) }),
        [typeof(Category)] = new AuditRule("Category", Hidden: new HashSet<string>(), Ignored: new HashSet<string>()),
        [typeof(Counterparty)] = new AuditRule("Counterparty", Hidden: new HashSet<string>(), Ignored: new HashSet<string>()),
        [typeof(PaymentMethod)] = new AuditRule("PaymentMethod", Hidden: new HashSet<string>(), Ignored: new HashSet<string>()),
        [typeof(Receipt)] = new AuditRule("Receipt", Hidden: new HashSet<string>(), Ignored: new HashSet<string>()),
        [typeof(ClosedYear)] = new AuditRule("ClosedYear", Hidden: new HashSet<string>(), Ignored: new HashSet<string>()),

        // A passkey sign-in updates the passkey data (sign count). Only create and delete are relevant.
        [typeof(IdentityUserPasskey<string>)] = new AuditRule(
            "Passkey",
            Hidden: new HashSet<string>(),
            Ignored: new HashSet<string> { nameof(IdentityUserPasskey<string>.Data) },
            AuditModified: false),
    };

    public static AuditRule? For(Type entityType) => rules.GetValueOrDefault(entityType);
}
