using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Gardens.Shared;

internal sealed record GardenResponse(Guid Id, string Code, string Name, string? Location, decimal? InstitutionalAreaM2, decimal? OperationalAreaM2, decimal? ExperimentalAreaM2, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class GardenProjection
{
    internal static System.Linq.Expressions.Expression<Func<Garden, GardenResponse>> Select => x => new(x.Id, x.Code, x.Name, x.Location, x.InstitutionalAreaM2, x.OperationalAreaM2, x.ExperimentalAreaM2, x.IsActive, x.Version, x.UpdatedAt);
    internal static GardenResponse Map(Garden x) => new(x.Id, x.Code, x.Name, x.Location, x.InstitutionalAreaM2, x.OperationalAreaM2, x.ExperimentalAreaM2, x.IsActive, x.Version, x.UpdatedAt);
}
