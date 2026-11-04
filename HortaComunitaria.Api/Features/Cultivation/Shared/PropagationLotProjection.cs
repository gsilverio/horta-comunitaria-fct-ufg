using HortaComunitaria.Api.Domain.Cultivation;
namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record PropagationLotResponse(Guid Id, Guid GardenId, string Code, Guid SpeciesId, Guid? CultivarId, MaterialType MaterialType, string? Origin, DateOnly? ReceivedOn, string? Notes, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class PropagationLotProjection
{
    internal static System.Linq.Expressions.Expression<Func<PropagationLot, PropagationLotResponse>> Select => x => new(x.Id, x.GardenId, x.Code, x.SpeciesId, x.CultivarId, x.MaterialType, x.Origin, x.ReceivedOn, x.Notes, x.IsActive, x.Version, x.UpdatedAt);
    internal static PropagationLotResponse Map(PropagationLot x) => new(x.Id, x.GardenId, x.Code, x.SpeciesId, x.CultivarId, x.MaterialType, x.Origin, x.ReceivedOn, x.Notes, x.IsActive, x.Version, x.UpdatedAt);
}
