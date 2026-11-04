using HortaComunitaria.Api.Domain.Cultivation;
namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record SpeciesResponse(Guid Id, string CommonName, string? ScientificName, string? Description, bool IsActive, int Version, DateTimeOffset? UpdatedAt, string? ImageUrl, int? VisualGrowthDays, int? VisualHarvestDays);
internal static class SpeciesProjection
{
    internal static System.Linq.Expressions.Expression<Func<Species, SpeciesResponse>> Select => x => new(x.Id, x.CommonName, x.ScientificName, x.Description, x.IsActive, x.Version, x.UpdatedAt, x.ImageBase64 == null ? null : "/api/v1/species/" + x.Id + "/image", x.VisualGrowthDays, x.VisualHarvestDays);
    internal static SpeciesResponse Map(Species x) => new(x.Id, x.CommonName, x.ScientificName, x.Description, x.IsActive, x.Version, x.UpdatedAt, x.ImageBase64 == null ? null : $"/api/v1/species/{x.Id}/image", x.VisualGrowthDays, x.VisualHarvestDays);
}
