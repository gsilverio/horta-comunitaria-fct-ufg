using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record HarvestResponse(Guid Id, Guid GardenId, Guid OperationId, Guid CycleId, DateOnly HarvestedOn, decimal FreshMassKg, decimal? UsableMassKg, string? LossReason, Guid? ResponsibleParticipantId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity)
{
    public decimal? AllocatedMassKg { get; init; }
    public decimal? RemainingUsableMassKg { get; init; }
}
internal sealed record CreatedHarvestResponse(bool Created, HarvestResponse Detail);
internal static class HarvestProjection
{
    internal static System.Linq.Expressions.Expression<Func<Harvest, HarvestResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.CycleId, x.HarvestedOn, x.FreshMassKg, x.UsableMassKg, x.LossReason, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static HarvestResponse Map(Harvest x) => new(x.Id, x.GardenId, x.OperationId, x.CycleId, x.HarvestedOn, x.FreshMassKg, x.UsableMassKg, x.LossReason, x.ResponsibleParticipantId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
