using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Occurrences.Shared;

internal sealed record OccurrenceResponse(Guid Id, Guid GardenId, Guid OperationId, DateTimeOffset OpenedAt, string Category, string Description, OccurrenceStatus Status, DateTimeOffset? ResolvedAt, string? Resolution, Guid? ResponsibleParticipantId, Guid? BedId, Guid? SectorId, Guid? CycleId, Guid? IrrigationId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedOccurrenceResponse(bool Created, OccurrenceResponse Detail);
internal static class OccurrenceProjection
{
    internal static System.Linq.Expressions.Expression<Func<Occurrence, OccurrenceResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.OpenedAt, x.Category, x.Description, x.Status, x.ResolvedAt, x.Resolution, x.ResponsibleParticipantId, x.BedId, x.SectorId, x.CycleId, x.IrrigationId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static OccurrenceResponse Map(Occurrence x) => new(x.Id, x.GardenId, x.OperationId, x.OpenedAt, x.Category, x.Description, x.Status, x.ResolvedAt, x.Resolution, x.ResponsibleParticipantId, x.BedId, x.SectorId, x.CycleId, x.IrrigationId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
