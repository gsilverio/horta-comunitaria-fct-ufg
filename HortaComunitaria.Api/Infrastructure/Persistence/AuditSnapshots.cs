using System.Text.Json;
using System.Text.Json.Serialization;
using HortaComunitaria.Api.Domain.History;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

/// <summary>Serialização explícita dos DTOs de domínio escolhidos pelos comandos, sem payload em logs.</summary>
internal static class AuditSnapshots
{
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    internal static string Capture<T>(T snapshot) => JsonSerializer.Serialize(snapshot, Options);
    internal static AuditEntry Create(Guid? gardenId, string entityType, Guid id, Guid actor, DateTimeOffset at,
        string? before, string after, string? reason = null, AuditOperation operation = AuditOperation.Created) =>
        AuditEntry.CreateRecord(gardenId, entityType, id, actor, at, operation, reason, before, after).Value;
}
