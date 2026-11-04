using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Costs.Shared;

internal static class CostsRecordContext
{
    internal static async Task<Result<Unit>> ResolveFinancialEntryAsync(AppDbContext db, Guid gardenId, FinancialEntryValues values, Guid id, CancellationToken token)
    {
        if (values.BedId is not null && !await db.Beds.AnyAsync(x => x.Id == values.BedId && x.GardenId == gardenId && x.IsActive, token) ||
            values.CycleId is not null && !await db.CultivationCycles.AnyAsync(x => x.Id == values.CycleId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token))
            return Result<Unit>.Failure(Error.NotFound());
        return Result<Unit>.Success(new());
    }
}
