using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Costs;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Costs.Shared;

internal sealed record FinancialEntryResponse(Guid Id, Guid GardenId, Guid OperationId, DateOnly EntryDate, string Description, string Category, decimal AmountBrl, FinancialNature Nature, Guid? BedId, Guid? CycleId, Guid RecordedByUserId, DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);
internal sealed record CreatedFinancialEntryResponse(bool Created, FinancialEntryResponse Detail);
internal static class FinancialEntryProjection
{
    internal static System.Linq.Expressions.Expression<Func<FinancialEntry, FinancialEntryResponse>> Select => x => new(x.Id, x.GardenId, x.OperationId, x.EntryDate, x.Description, x.Category, x.AmountBrl, x.Nature, x.BedId, x.CycleId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
    internal static FinancialEntryResponse Map(FinancialEntry x) => new(x.Id, x.GardenId, x.OperationId, x.EntryDate, x.Description, x.Category, x.AmountBrl, x.Nature, x.BedId, x.CycleId, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}
