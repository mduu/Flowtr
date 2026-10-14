using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Flowtr.Server.CrossCutting.Audit;

/// <summary>
/// Adds an <see cref="AuditEntry"/> for each change of an audited entity (<see cref="AuditRules"/>).
/// The entries go into the same <c>SaveChanges</c>, thus into the same transaction as the change.
/// </summary>
/// <remarks>Audited entities must get their key before the save (for example a GUID), else a new entity has no ID in its entry.</remarks>
public sealed class AuditInterceptor(IAuditActor actor, TimeProvider time) : SaveChangesInterceptor
{
    // Enums as names: the audit log must stay readable when an enum gets new values.
    private static readonly JsonSerializerOptions jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddEntries(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddEntries(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => RemoveUnsavedEntries(eventData.Context);

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        RemoveUnsavedEntries(eventData.Context);
        return Task.CompletedTask;
    }

    /// <summary>
    /// After a failed save, the entries of that save must go: a second attempt adds them again.
    /// </summary>
    private static void RemoveUnsavedEntries(DbContext? context)
    {
        foreach (var entry in context?.ChangeTracker.Entries<AuditEntry>().Where(e => e.State == EntityState.Added).ToList() ?? [])
        {
            entry.State = EntityState.Detached;
        }
    }

    private void AddEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var changes = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (changes.Any(entry => entry.Entity is AuditEntry && entry.State != EntityState.Added))
        {
            throw new InvalidOperationException("Audit entries are read-only.");
        }

        var (userId, userName) = actor.Current;
        var now = time.GetUtcNow();

        foreach (var change in changes)
        {
            if (AuditRules.For(change.Metadata.ClrType) is not { } rule || ToAction(change.State) is not { } action)
            {
                continue;
            }

            if (action == AuditAction.Modified && !rule.AuditModified)
            {
                continue;
            }

            var properties = ChangedProperties(change, action, rule);
            if (action == AuditAction.Modified && properties.Count == 0)
            {
                continue;
            }

            context.Add(new AuditEntry
            {
                At = now,
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = rule.Key,
                EntityId = KeyText(change),
                Changes = JsonSerializer.Serialize(properties, jsonOptions),
            });
        }
    }

    private static AuditAction? ToAction(EntityState state) => state switch
    {
        EntityState.Added => AuditAction.Created,
        EntityState.Modified => AuditAction.Modified,
        EntityState.Deleted => AuditAction.Deleted,
        _ => null,
    };

    private static Dictionary<string, AuditValue> ChangedProperties(EntityEntry change, AuditAction action, AuditRule rule)
    {
        var properties = new Dictionary<string, AuditValue>();

        foreach (var property in change.Properties)
        {
            var name = property.Metadata.Name;
            if (rule.Ignored.Contains(name))
            {
                continue;
            }

            var old = action == AuditAction.Created ? null : property.OriginalValue;
            var @new = action == AuditAction.Deleted ? null : property.CurrentValue;

            if (action == AuditAction.Modified && Equals(old, @new))
            {
                continue;
            }

            properties[name] = rule.Hidden.Contains(name)
                ? new AuditValue(Hide(old), Hide(@new))
                : new AuditValue(old, @new);
        }

        return properties;
    }

    private static string KeyText(EntityEntry change) =>
        string.Join("/", change.Metadata.FindPrimaryKey()!.Properties
            .Select(key => change.Property(key.Name).CurrentValue switch
            {
                byte[] bytes => Convert.ToBase64String(bytes),
                var value => value?.ToString(),
            }));

    private static string? Hide(object? value) => value is null ? null : AuditRules.HiddenValue;
}

/// <summary>
/// Old and new value of one property in <see cref="AuditEntry.Changes"/>.
/// </summary>
public sealed record AuditValue(object? Old, object? New);
