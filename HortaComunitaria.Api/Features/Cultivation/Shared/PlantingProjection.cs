using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record PlantingResponse(Guid Id, Guid GardenId, Guid OperationId, Guid CycleId, Guid? PropagationLotId, Guid? ReplacedPlantingId, DateOnly PlantedOn, PlantingKind Kind, int PlantedQuantity, decimal? PlantSpacingCm, decimal? RowSpacingCm, Guid? ResponsibleParticipantId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedPlantingResponse(bool Created, PlantingResponse Detail);
internal static class PlantingProjection
{
    internal static System.Linq.Expressions.Expression<Func<Planting, PlantingResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.CycleId, x.PropagationLotId, x.ReplacedPlantingId, x.PlantedOn, x.Kind, x.PlantedQuantity, x.PlantSpacingCm, x.RowSpacingCm, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static PlantingResponse Map(Planting x) => new(x.Id, x.GardenId, x.OperationId, x.CycleId, x.PropagationLotId, x.ReplacedPlantingId, x.PlantedOn, x.Kind, x.PlantedQuantity, x.PlantSpacingCm, x.RowSpacingCm, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
