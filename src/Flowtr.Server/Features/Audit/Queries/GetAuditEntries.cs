using System.Globalization;
using System.Text.Json;
using Flowtr.Server.CrossCutting.Audit;
using Flowtr.Server.CrossCutting.Cqs;
using Flowtr.Server.Data;
using Flowtr.Shared;
using Flowtr.Shared.Audit;
using Flowtr.Shared.Journal;
using Microsoft.EntityFrameworkCore;

namespace Flowtr.Server.Features.Audit.Queries;

/// <summary>
/// One page of the audit log, newest first, with German texts and Swiss formats.
/// </summary>
public sealed record GetAuditEntries(AuditFilter Filter);

public sealed class GetAuditEntriesHandler(ApplicationDbContext db) : IQueryHandler<GetAuditEntries, AuditPage>
{
    private const int MaxTake = 200;

    private static readonly TimeZoneInfo swissTime = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

    private static readonly Dictionary<string, string> propertyLabels = new()
    {
        ["Email"] = "E-Mail",
        ["UserName"] = "Benutzername",
        ["EmailConfirmed"] = "E-Mail bestätigt",
        ["InvitedAt"] = "Eingeladen am",
        ["PasswordHash"] = "Passwort",
        ["SecurityStamp"] = "Sicherheitsstempel",
        ["LockoutEnd"] = "Gesperrt bis",
        ["LockoutEnabled"] = "Sperre möglich",
        ["AccessFailedCount"] = "Fehlversuche",
        ["PhoneNumber"] = "Telefon",
        ["RoleId"] = "Rolle",
        ["UserId"] = "Benutzer",
        ["CredentialId"] = "Passkey-ID",
        ["Date"] = "Datum",
        ["Kind"] = "Art",
        ["AmountChf"] = "Betrag CHF",
        ["OriginalAmount"] = "Originalbetrag",
        ["OriginalCurrency"] = "Währung",
        ["Description"] = "Beschreibung",
        ["CategoryId"] = "Kategorie",
        ["CounterpartyId"] = "Gegenpartei",
        ["PaymentMethodId"] = "Zahlungsart",
        ["ReceiptNumber"] = "Belegnummer",
        ["Note"] = "Notiz",
        ["VatRate"] = "MWST-Satz",
        ["VatAmount"] = "MWST-Betrag",
        ["Name"] = "Name",
        ["IsActive"] = "Aktiv",
        ["SortOrder"] = "Reihenfolge",
        ["Year"] = "Jahr",
        ["ClosedAt"] = "Abgeschlossen am",
    };

    private static readonly HashSet<string> amountProperties = ["AmountChf", "OriginalAmount", "VatAmount"];

    private static readonly Dictionary<AuditAction, string> actionLabels = new()
    {
        [AuditAction.Created] = "Erstellt",
        [AuditAction.Modified] = "Geändert",
        [AuditAction.Deleted] = "Gelöscht",
    };

    public async Task<Result<AuditPage>> Handle(GetAuditEntries query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        var entries = db.AuditEntries.AsNoTracking();

        if (filter.From is { } from)
        {
            entries = entries.Where(entry => entry.At >= from);
        }

        if (filter.To is { } to)
        {
            entries = entries.Where(entry => entry.At <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            entries = entries.Where(entry => entry.UserName.Contains(filter.User.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Area))
        {
            entries = entries.Where(entry => entry.EntityType == filter.Area);
        }

        var total = await entries.CountAsync(cancellationToken);
        var page = await entries
            .OrderByDescending(entry => entry.At)
            .ThenByDescending(entry => entry.Id)
            .Skip(Math.Max(filter.Skip, 0))
            .Take(Math.Clamp(filter.Take, 1, MaxTake))
            .ToListAsync(cancellationToken);

        var names = new Names(
            await UserEmails(page, cancellationToken),
            await db.Roles.AsNoTracking().ToDictionaryAsync(role => role.Id, role => role.Name ?? role.Id, cancellationToken),
            await EntityNames(page, cancellationToken));

        return Result.Success(new AuditPage(page.Select(names.View).ToList(), total));
    }

    /// <summary>
    /// E-mail addresses of the users in <paramref name="page"/>. For deleted users, the address comes from their audit entries.
    /// </summary>
    private async Task<Dictionary<string, string>> UserEmails(List<AuditEntry> page, CancellationToken cancellationToken)
    {
        var emails = await db.Users.AsNoTracking().ToDictionaryAsync(user => user.Id, user => user.Email ?? user.Id, cancellationToken);

        var userIds = page.SelectMany(entry => entry.EntityType switch
        {
            "User" => [entry.EntityId],
            "UserRole" => [entry.EntityId.Split('/')[0]],
            _ => Array.Empty<string>(),
        });

        await AddFromHistory(emails, "User", userIds, changes => Value(changes, "Email"), cancellationToken);
        return emails;
    }

    /// <summary>
    /// Names of journal entities by ID (also for the IDs in the changes), from the database or, if deleted, from their audit entries.
    /// </summary>
    private async Task<Dictionary<string, string>> EntityNames(List<AuditEntry> page, CancellationToken cancellationToken)
    {
        var names = new Dictionary<string, string>();

        foreach (var category in await db.Categories.AsNoTracking().ToListAsync(cancellationToken))
        {
            names[category.Id.ToString()] = category.Name;
        }

        foreach (var counterparty in await db.Counterparties.AsNoTracking().ToListAsync(cancellationToken))
        {
            names[counterparty.Id.ToString()] = counterparty.Name;
        }

        foreach (var method in await db.PaymentMethods.AsNoTracking().ToListAsync(cancellationToken))
        {
            names[method.Id.ToString()] = method.Name;
        }

        var bookingIds = page.Where(entry => entry.EntityType == "Booking").Select(entry => Guid.Parse(entry.EntityId)).Distinct().ToList();
        foreach (var booking in await db.Bookings.AsNoTracking().Where(b => bookingIds.Contains(b.Id)).ToListAsync(cancellationToken))
        {
            names[booking.Id.ToString()] = $"{SwissFormat.Date(booking.Date)} · {booking.Description}";
        }

        await AddFromHistory(names, "Booking", bookingIds.Select(id => id.ToString()), BookingName, cancellationToken);
        foreach (var area in new[] { "Category", "Counterparty", "PaymentMethod" })
        {
            await AddFromHistory(names, area, page.Where(entry => entry.EntityType == area).Select(entry => entry.EntityId),
                changes => Value(changes, "Name"), cancellationToken);
        }

        return names;
    }

    private async Task AddFromHistory(
        Dictionary<string, string> names,
        string entityType,
        IEnumerable<string> ids,
        Func<Dictionary<string, AuditValue<JsonElement>>, string?> name,
        CancellationToken cancellationToken)
    {
        var missing = ids.Where(id => !names.ContainsKey(id)).Distinct().ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var history = await db.AuditEntries.AsNoTracking()
            .Where(entry => entry.EntityType == entityType && missing.Contains(entry.EntityId))
            .OrderByDescending(entry => entry.Id)
            .Select(entry => new { entry.EntityId, entry.Changes })
            .ToListAsync(cancellationToken);

        foreach (var entry in history)
        {
            if (name(Parse(entry.Changes)) is { } value)
            {
                names.TryAdd(entry.EntityId, value);
            }
        }
    }

    private static string? BookingName(Dictionary<string, AuditValue<JsonElement>> changes) =>
        Value(changes, "Description") is { } description
            ? Value(changes, "Date") is { } date && DateOnly.TryParse(date, CultureInfo.InvariantCulture, out var day)
                ? $"{SwissFormat.Date(day)} · {description}"
                : description
            : null;

    private static string? Value(Dictionary<string, AuditValue<JsonElement>> changes, string property) =>
        changes.TryGetValue(property, out var value) ? Names.Text(value.New) ?? Names.Text(value.Old) : null;

    private static Dictionary<string, AuditValue<JsonElement>> Parse(string changes) =>
        JsonSerializer.Deserialize<Dictionary<string, AuditValue<JsonElement>>>(changes) ?? [];

    private sealed record Names(Dictionary<string, string> Users, Dictionary<string, string> Roles, Dictionary<string, string> Entities)
    {
        public AuditEntryView View(AuditEntry entry)
        {
            var changes = Parse(entry.Changes);

            return new AuditEntryView(
                entry.Id,
                entry.At,
                entry.UserName,
                actionLabels[entry.Action],
                AuditAreas.Labels.GetValueOrDefault(entry.EntityType, entry.EntityType),
                Subject(entry, changes),
                changes
                    .Where(change => IsShown(entry, change.Key, change.Value))
                    .Select(change => new AuditChangeView(
                        propertyLabels.GetValueOrDefault(change.Key, change.Key),
                        Format(change.Key, change.Value.Old),
                        Format(change.Key, change.Value.New)))
                    .ToList());
        }

        private string Subject(AuditEntry entry, Dictionary<string, AuditValue<JsonElement>> changes) => entry.EntityType switch
        {
            "User" => User(entry.EntityId, changes),
            "UserRole" when entry.EntityId.Split('/') is [var userId, var roleId] =>
                $"{User(userId, [])}: {Roles.GetValueOrDefault(roleId, roleId)}",
            "Passkey" when changes.TryGetValue("UserId", out var user) => User(Text(user.Old) ?? Text(user.New) ?? "", []),
            "ClosedYear" => entry.EntityId,
            _ => Entities.GetValueOrDefault(entry.EntityId, entry.EntityId),
        };

        private string User(string userId, Dictionary<string, AuditValue<JsonElement>> changes) =>
            Users.GetValueOrDefault(userId)
            ?? (changes.TryGetValue("Email", out var email) ? Text(email.Old) ?? Text(email.New) : null)
            ?? userId;

        private string? Format(string property, JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => "Ja",
            JsonValueKind.False => "Nein",
            JsonValueKind.Number when amountProperties.Contains(property) => SwissFormat.Amount(value.GetDecimal()),
            JsonValueKind.String when value.GetString() == AuditRules.HiddenValue => "verborgen",
            JsonValueKind.String when property == "RoleId" => Roles.GetValueOrDefault(value.GetString()!, value.GetString()!),
            JsonValueKind.String when property == "UserId" => Users.GetValueOrDefault(value.GetString()!, value.GetString()!),
            JsonValueKind.String when property.EndsWith("Id") => Entities.GetValueOrDefault(value.GetString()!, value.GetString()!),
            JsonValueKind.String when property == "Kind" => value.GetString() == nameof(BookingKind.Income) ? "Einnahme" : "Ausgabe",
            JsonValueKind.String when DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) =>
                SwissFormat.Date(day),
            JsonValueKind.String when value.TryGetDateTimeOffset(out var date) =>
                TimeZoneInfo.ConvertTime(date, swissTime).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            _ => Text(value) ?? value.GetRawText(),
        };

        /// <summary>
        /// Leaves out what the subject already shows, and empty or hidden values of created or deleted entities.
        /// </summary>
        private static bool IsShown(AuditEntry entry, string property, AuditValue<JsonElement> value) =>
            property != "Id"
            && !(entry.EntityType is "UserRole" or "Passkey" && property == "UserId")
            && (entry.Action == AuditAction.Modified || (HasContent(value) && !IsHidden(value)));

        private static bool IsHidden(AuditValue<JsonElement> value) =>
            Text(value.Old) == AuditRules.HiddenValue || Text(value.New) == AuditRules.HiddenValue;

        private static bool HasContent(AuditValue<JsonElement> value) =>
            IsContent(value.Old) || IsContent(value.New);

        private static bool IsContent(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined or JsonValueKind.False => false,
            JsonValueKind.Number => value.GetRawText() != "0",
            _ => true,
        };

        public static string? Text(JsonElement value) =>
            value.ValueKind == JsonValueKind.String ? value.GetString() : value.ValueKind is JsonValueKind.Number ? value.GetRawText() : null;
    }

    private sealed record AuditValue<T>(T Old, T New);
}
