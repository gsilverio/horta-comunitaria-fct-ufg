using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record ActivityResponse(Guid Id, Guid GardenId, Guid OperationId, string Title, string Category, DateOnly PerformedOn, string? Description, Guid? BedId, Guid? SectorId, Guid? CycleId, Guid? OccurrenceId, bool IsOperational, bool IsSocial, bool IsEducational, string? Objective, string? Modality, int? ReportedAudienceTotal, bool? AudienceListComplete, IReadOnlyList<ActivityParticipantResponse> Participants, IReadOnlyList<InputUsageResponse> InputUsages, IReadOnlyList<ActivityInstitutionResponse> Institutions, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedActivityResponse(bool Created, ActivityResponse Detail);
internal static class ActivityProjection
{
    internal static System.Linq.Expressions.Expression<Func<Activity, ActivityResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.Title, x.Category, x.PerformedOn, x.Description, x.BedId, x.SectorId, x.CycleId, x.OccurrenceId, x.IsOperational, x.IsSocial, x.IsEducational, x.Objective, x.Modality, x.ReportedAudienceTotal, x.AudienceListComplete, x.Participants.Select(item => new ActivityParticipantResponse(item.ParticipantId, item.Role, item.WorkHours, item.CountsAsAudience)).ToArray(), x.InputUsages.Select(item => new InputUsageResponse(item.Id, item.InputId, item.Quantity, item.Unit)).ToArray(), x.Institutions.Select(item => new ActivityInstitutionResponse(item.InstitutionId, item.Role)).ToArray(), x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static ActivityResponse Map(Activity x) => new(x.Id, x.GardenId, x.OperationId, x.Title, x.Category, x.PerformedOn, x.Description, x.BedId, x.SectorId, x.CycleId, x.OccurrenceId, x.IsOperational, x.IsSocial, x.IsEducational, x.Objective, x.Modality, x.ReportedAudienceTotal, x.AudienceListComplete, x.Participants.Select(item => new ActivityParticipantResponse(item.ParticipantId, item.Role, item.WorkHours, item.CountsAsAudience)).ToArray(), x.InputUsages.Select(item => new InputUsageResponse(item.Id, item.InputId, item.Quantity, item.Unit)).ToArray(), x.Institutions.Select(item => new ActivityInstitutionResponse(item.InstitutionId, item.Role)).ToArray(), x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
