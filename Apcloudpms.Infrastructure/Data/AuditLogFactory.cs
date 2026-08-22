using Apcloudpms.Application.Interfaces;
using Apcloudpms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace Apcloudpms.Infrastructure.Data;

internal static class AuditLogFactory
{
    private const string RedactedValue = "[REDACTED]";
    private static readonly string[] SensitiveTerms = ["password", "token", "secret", "hash"];

    public static IReadOnlyList<AuditLog> Create(ChangeTracker changeTracker, IAuditContext auditContext)
    {
        changeTracker.DetectChanges();
        var changedAtUtc = DateTime.UtcNow;

        return changeTracker.Entries()
            .Where(entry => entry.Entity is not AuditLog &&
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(entry => CreateLog(entry, auditContext, changedAtUtc))
            .Where(log => log is not null)
            .Cast<AuditLog>()
            .ToList();
    }

    private static AuditLog? CreateLog(
        EntityEntry entry,
        IAuditContext auditContext,
        DateTime changedAtUtc)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changedColumns = new List<string>();

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey())
                continue;

            var isSensitive = SensitiveTerms.Any(term =>
                propertyName.Contains(term, StringComparison.OrdinalIgnoreCase));
            var oldValue = isSensitive ? RedactedValue : property.OriginalValue;
            var newValue = isSensitive ? RedactedValue : property.CurrentValue;

            if (entry.State == EntityState.Added)
            {
                newValues[propertyName] = newValue;
                changedColumns.Add(propertyName);
            }
            else if (entry.State == EntityState.Deleted)
            {
                oldValues[propertyName] = oldValue;
                changedColumns.Add(propertyName);
            }
            else if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
            {
                oldValues[propertyName] = oldValue;
                newValues[propertyName] = newValue;
                changedColumns.Add(propertyName);
            }
        }

        if (entry.State == EntityState.Modified && changedColumns.Count == 0)
            return null;

        var keyValues = entry.Properties
            .Where(property => property.Metadata.IsPrimaryKey())
            .ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);

        return new AuditLog
        {
            EntityName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
            Action = entry.State.ToString(),
            EntityKey = Serialize(keyValues),
            ChangedColumns = Serialize(changedColumns),
            OldValues = oldValues.Count == 0 ? null : Serialize(oldValues),
            NewValues = newValues.Count == 0 ? null : Serialize(newValues),
            ChangedByUserId = auditContext.UserId,
            ChangedBy = auditContext.UserName,
            TraceId = auditContext.TraceId,
            IpAddress = auditContext.IpAddress,
            ChangedAtUtc = changedAtUtc
        };
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value);
}
