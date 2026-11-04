using HortaComunitaria.Api.Domain.Cultivation;
namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record CultivarResponse(Guid Id, Guid SpeciesId, string Name, string? Description, bool IsActive, int Version, DateTimeOffset? UpdatedAt, int? VisualGrowthDays, int? VisualHarvestDays);
internal static class CultivarProjection
{
    internal static System.Linq.Expressions.Expression<Func<Cultivar, CultivarResponse>> Select => x => new(x.Id, x.SpeciesId, x.Name, x.Description, x.IsActive, x.Version, x.UpdatedAt, x.VisualGrowthDays, x.VisualHarvestDays);
    internal static CultivarResponse Map(Cultivar x) => new(x.Id, x.SpeciesId, x.Name, x.Description, x.IsActive, x.Version, x.UpdatedAt, x.VisualGrowthDays, x.VisualHarvestDays);
}
