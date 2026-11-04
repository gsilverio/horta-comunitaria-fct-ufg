using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record AgronomicObservationResponse(Guid Id, Guid GardenId, Guid OperationId, Guid CycleId, DateOnly ObservedOn, string Category, string Description, decimal? Measurement, string? Unit, string? Method, Guid? ResponsibleParticipantId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedAgronomicObservationResponse(bool Created, AgronomicObservationResponse Detail);
internal static class AgronomicObservationProjection
{
    internal static System.Linq.Expressions.Expression<Func<AgronomicObservation, AgronomicObservationResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.CycleId, x.ObservedOn, x.Category, x.Description, x.Measurement, x.Unit, x.Method, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static AgronomicObservationResponse Map(AgronomicObservation x) => new(x.Id, x.GardenId, x.OperationId, x.CycleId, x.ObservedOn, x.Category, x.Description, x.Measurement, x.Unit, x.Method, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
