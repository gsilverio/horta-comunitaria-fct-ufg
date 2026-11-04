using HortaComunitaria.Api.Domain.Gardens;
namespace HortaComunitaria.Api.Features.Gardens.Shared;

internal sealed record BedSectorAssignmentResponse(Guid Id, Guid GardenId, Guid BedId, Guid SectorId, DateOnly ValidFrom, DateOnly? ValidUntil);
internal static class BedSectorAssignmentProjection
{
    internal static System.Linq.Expressions.Expression<Func<BedSectorAssignment, BedSectorAssignmentResponse>> Select => x => new(x.Id, x.GardenId, x.BedId, x.IrrigationSectorId, x.ValidFrom, x.ValidUntil);
    internal static BedSectorAssignmentResponse Map(BedSectorAssignment x) => new(x.Id, x.GardenId, x.BedId, x.IrrigationSectorId, x.ValidFrom, x.ValidUntil);
}
