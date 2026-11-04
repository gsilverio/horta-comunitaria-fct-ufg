using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record EstablishmentAssessmentResponse(Guid Id, Guid GardenId, Guid OperationId, Guid PlantingId, DateOnly AssessedOn, int SurvivingQuantity, string Method, string? Notes, Guid? ResponsibleParticipantId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedEstablishmentAssessmentResponse(bool Created, EstablishmentAssessmentResponse Detail);
internal static class EstablishmentAssessmentProjection
{
    internal static System.Linq.Expressions.Expression<Func<EstablishmentAssessment, EstablishmentAssessmentResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.PlantingId, x.AssessedOn, x.SurvivingQuantity, x.Method, x.Notes, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static EstablishmentAssessmentResponse Map(EstablishmentAssessment x) => new(x.Id, x.GardenId, x.OperationId, x.PlantingId, x.AssessedOn, x.SurvivingQuantity, x.Method, x.Notes, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
