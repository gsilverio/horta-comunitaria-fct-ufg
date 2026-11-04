using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record CultivationCycleResponse(Guid Id, Guid GardenId, Guid OperationId, Guid BedId, Guid SpeciesId, Guid? CultivarId, DateOnly StartedOn, decimal OccupiedAreaM2, DateOnly? EndedOn, Guid? ResponsibleParticipantId, Guid BedConfigurationId, decimal ReferenceAreaM2, CultivationStatus Status, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedCultivationCycleResponse(bool Created, CultivationCycleResponse Detail);
internal static class CultivationCycleProjection
{
    internal static System.Linq.Expressions.Expression<Func<CultivationCycle, CultivationCycleResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.BedId, x.SpeciesId, x.CultivarId, x.StartedOn, x.OccupiedAreaM2, x.EndedOn, x.ResponsibleParticipantId, x.BedConfigurationId, x.ReferenceAreaM2, x.Status, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static CultivationCycleResponse Map(CultivationCycle x) => new(x.Id, x.GardenId, x.OperationId, x.BedId, x.SpeciesId, x.CultivarId, x.StartedOn, x.OccupiedAreaM2, x.EndedOn, x.ResponsibleParticipantId, x.BedConfigurationId, x.ReferenceAreaM2, x.Status, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
