using HortaComunitaria.Api.Domain.Environment;
namespace HortaComunitaria.Api.Features.Environment.Shared;

internal sealed record SamplingPointResponse(Guid Id, Guid GardenId, string Code, string Description, string? Location, Guid? BedId, Guid? SectorId, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class SamplingPointProjection
{
    internal static System.Linq.Expressions.Expression<Func<SamplingPoint, SamplingPointResponse>> Select => x => new(x.Id, x.GardenId, x.Code, x.Description, x.Location, x.BedId, x.SectorId, x.IsActive, x.Version, x.UpdatedAt);
    internal static SamplingPointResponse Map(SamplingPoint x) => new(x.Id, x.GardenId, x.Code, x.Description, x.Location, x.BedId, x.SectorId, x.IsActive, x.Version, x.UpdatedAt);
}
