using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal static class DryingContext
{
    internal static async Task<Result<Unit>> ResolveAsync(AppDbContext db, Guid gardenId, DryingLotValues values, Guid id, CancellationToken token)
    {
        if (await db.DryingLots.AnyAsync(x => x.GardenId == gardenId && x.Code == values.Code && x.Id != id, token))
            return Result<Unit>.Failure(new("catalog.code_conflict", "Código de lote já utilizado na horta.", ErrorCategory.Conflict));
        if (values.ResponsibleParticipantId is not null && !await db.Participants.AnyAsync(x => x.Id == values.ResponsibleParticipantId && x.GardenId == gardenId && x.IsActive, token)) return Result<Unit>.Failure(Error.NotFound());
        var ids = values.Items.Select(x => x.HarvestId).ToArray();
        await WriteLocks.HarvestsAsync(db, gardenId, ids, token);
        var harvests = await db.Harvests.AsNoTracking().Where(x => ids.Contains(x.Id) && x.GardenId == gardenId && x.Validity == RecordValidity.Valid).ToDictionaryAsync(x => x.Id, token);
        if (harvests.Count != ids.Length) return Result<Unit>.Failure(Error.NotFound());
        var allocated = await db.DryingItems.AsNoTracking().Where(x => x.GardenId == gardenId && ids.Contains(x.HarvestId) && x.DryingLotId != id && db.DryingLots.Any(l => l.Id == x.DryingLotId && l.Validity == RecordValidity.Valid))
            .GroupBy(x => x.HarvestId).Select(g => new { Id = g.Key, Mass = g.Sum(x => x.AllocatedMassKg) }).ToDictionaryAsync(x => x.Id, x => x.Mass, token);
        foreach (var item in values.Items)
        {
            var harvest = harvests[item.HarvestId];
            if (harvest.HarvestedOn > values.StartedOn) return Result<Unit>.Failure(Error.Validation("startedOn", "Destinação não pode preceder a colheita."));
            var error = CultivationRules.Allocation(harvest.UsableMassKg, allocated.GetValueOrDefault(item.HarvestId), item.AllocatedMassKg);
            if (error is not null) return Result<Unit>.Failure(error);
        }
        return Result<Unit>.Success(new());
    }
}
