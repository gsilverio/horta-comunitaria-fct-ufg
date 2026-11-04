using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Gardens.Shared;

internal sealed record IrrigationSectorResponse(Guid Id, Guid GardenId, string Code, string Name, string? Description, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class IrrigationSectorProjection
{
    internal static System.Linq.Expressions.Expression<Func<IrrigationSector, IrrigationSectorResponse>> Select => x => new(x.Id, x.GardenId, x.Code, x.Name, x.Description, x.IsActive, x.Version, x.UpdatedAt);
    internal static IrrigationSectorResponse Map(IrrigationSector x) => new(x.Id, x.GardenId, x.Code, x.Name, x.Description, x.IsActive, x.Version, x.UpdatedAt);
}
