using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Gardens.Shared;

internal sealed record BedResponse(Guid Id, Guid GardenId, string Code, string Name, string? Location, bool IsActive, int Version, DateTimeOffset? UpdatedAt)
{
    public DateOnly? On { get; init; }
    public BedConfigurationResponse? Configuration { get; init; }
    public BedSectorAssignmentResponse? SectorAssignment { get; init; }
}
internal static class BedProjection
{
    internal static System.Linq.Expressions.Expression<Func<Bed, BedResponse>> Select => x => new(x.Id, x.GardenId, x.Code, x.Name, x.Location, x.IsActive, x.Version, x.UpdatedAt);
    internal static BedResponse Map(Bed x) => new(x.Id, x.GardenId, x.Code, x.Name, x.Location, x.IsActive, x.Version, x.UpdatedAt);
}
