using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record DryingLotResponse(Guid Id, Guid GardenId, Guid OperationId, string Code, string? Method, DateOnly StartedOn, DateOnly? EndedOn, decimal? TotalDryMassKg, Guid? ResponsibleParticipantId, IReadOnlyList<DryingItemResponse> Items, DryingStatus Status, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedDryingLotResponse(bool Created, DryingLotResponse Detail);
internal static class DryingLotProjection
{
    internal static System.Linq.Expressions.Expression<Func<DryingLot, DryingLotResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.Code, x.Method, x.StartedOn, x.EndedOn, x.TotalDryMassKg, x.ResponsibleParticipantId, x.Items.Select(item => new DryingItemResponse(item.HarvestId, item.AllocatedMassKg)).ToArray(), x.Status, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static DryingLotResponse Map(DryingLot x) => new(x.Id, x.GardenId, x.OperationId, x.Code, x.Method, x.StartedOn, x.EndedOn, x.TotalDryMassKg, x.ResponsibleParticipantId, x.Items.Select(item => new DryingItemResponse(item.HarvestId, item.AllocatedMassKg)).ToArray(), x.Status, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
