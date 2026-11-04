using System.Text.Json;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Infrastructure.Persistence;
namespace HortaComunitaria.Api.Features.Gardens.Shared;
internal sealed record LayoutResponse(int Version, DateTimeOffset? SavedAt, LayoutDocument? Layout);
internal static class LayoutProjection
{
    internal static LayoutResponse Map(GardenLayout entity) => new(entity.Version, entity.SavedAt, JsonSerializer.Deserialize<LayoutDocument>(entity.Document, AuditSnapshots.Options));
}
