using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Environment;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Environment.Shared;

internal sealed record EnvironmentalMeasurementResponse(Guid Id, Guid GardenId, Guid OperationId, Guid SamplingPointId, Guid VariableId, DateTimeOffset CollectedAt, decimal Value, string Unit, string Method, MeasurementQuality Quality, Guid? ResponsibleParticipantId, string? Notes, Guid? BedId, Guid? SectorId, Guid? BedSectorAssignmentId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedEnvironmentalMeasurementResponse(bool Created, EnvironmentalMeasurementResponse Detail);
internal static class EnvironmentalMeasurementProjection
{
    internal static System.Linq.Expressions.Expression<Func<EnvironmentalMeasurement, EnvironmentalMeasurementResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.SamplingPointId, x.VariableId, x.CollectedAt, x.Value, x.Unit, x.Method, x.Quality, x.ResponsibleParticipantId, x.Notes, x.BedId, x.SectorId, x.BedSectorAssignmentId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static EnvironmentalMeasurementResponse Map(EnvironmentalMeasurement x) => new(x.Id, x.GardenId, x.OperationId, x.SamplingPointId, x.VariableId, x.CollectedAt, x.Value, x.Unit, x.Method, x.Quality, x.ResponsibleParticipantId, x.Notes, x.BedId, x.SectorId, x.BedSectorAssignmentId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
