using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal static class CultivationDependencies
{
    internal static async Task<Result<Unit>> CheckCultivationCycleAsync(AppDbContext db, Guid gardenId, Guid id, CultivationCycleValues? proposed, CancellationToken token)
    {
        var plantings = db.Plantings.Where(x => x.GardenId == gardenId && x.CycleId == id && x.Validity == RecordValidity.Valid);
        var observations = db.AgronomicObservations.Where(x => x.GardenId == gardenId && x.CycleId == id && x.Validity == RecordValidity.Valid);
        var harvests = db.Harvests.Where(x => x.GardenId == gardenId && x.CycleId == id && x.Validity == RecordValidity.Valid);
        var other = await db.Activities.AnyAsync(x => x.GardenId == gardenId && x.CycleId == id && x.Validity == RecordValidity.Valid, token) ||
            await db.Occurrences.AnyAsync(x => x.GardenId == gardenId && x.CycleId == id && x.Validity == RecordValidity.Valid, token) ||
            await db.FinancialEntries.AnyAsync(x => x.GardenId == gardenId && x.CycleId == id && x.Validity == RecordValidity.Valid, token);
        var hasFacts = await plantings.AnyAsync(token) || await observations.AnyAsync(token) || await harvests.AnyAsync(token);
        if (proposed is null) return hasFacts || other
            ? Result<Unit>.Failure(RecordRules.HasDependents("Ciclo possui fatos válidos; revise os dependentes antes de invalidar.")) : Result<Unit>.Success(new());
        var current = await db.CultivationCycles.AsNoTracking().SingleAsync(x => x.Id == id && x.GardenId == gardenId, token);
        if ((hasFacts || other) && (proposed.SpeciesId != current.SpeciesId || proposed.CultivarId != current.CultivarId || proposed.BedId != current.BedId) ||
            await plantings.AnyAsync(x => x.PlantedOn < proposed.StartedOn || proposed.EndedOn != null && x.PlantedOn > proposed.EndedOn, token) ||
            await observations.AnyAsync(x => x.ObservedOn < proposed.StartedOn || proposed.EndedOn != null && x.ObservedOn > proposed.EndedOn, token) ||
            await harvests.AnyAsync(x => x.HarvestedOn < proposed.StartedOn || proposed.EndedOn != null && x.HarvestedOn > proposed.EndedOn, token))
            return Result<Unit>.Failure(RecordRules.HasDependents("Mudança de contexto ou período contradiz fatos válidos do ciclo; revise-os explicitamente."));
        return Result<Unit>.Success(new());
    }

    internal static async Task<Result<Unit>> CheckPlantingAsync(AppDbContext db, Guid gardenId, Guid id, PlantingValues? proposed, CancellationToken token)
    {
        var assessments = db.EstablishmentAssessments.Where(x => x.GardenId == gardenId && x.PlantingId == id && x.Validity == RecordValidity.Valid);
        var replacements = db.Plantings.Where(x => x.GardenId == gardenId && x.ReplacedPlantingId == id && x.Validity == RecordValidity.Valid);
        if (proposed is null) return await assessments.AnyAsync(token) || await replacements.AnyAsync(token)
            ? Result<Unit>.Failure(RecordRules.HasDependents("Plantio possui avaliações ou reposições válidas.")) : Result<Unit>.Success(new());
        var current = await db.Plantings.AsNoTracking().SingleAsync(x => x.Id == id && x.GardenId == gardenId, token);
        if (await assessments.AnyAsync(x => x.SurvivingQuantity > proposed.PlantedQuantity || x.AssessedOn < proposed.PlantedOn, token) ||
            await replacements.AnyAsync(x => x.CycleId != proposed.CycleId || x.PlantedOn < proposed.PlantedOn, token) ||
            current.CycleId != proposed.CycleId && await assessments.AnyAsync(token))
            return Result<Unit>.Failure(RecordRules.HasDependents("Quantidade, data ou ciclo propostos contradizem avaliações/reposições válidas."));
        return Result<Unit>.Success(new());
    }

    internal static async Task<Result<Unit>> CheckHarvestAsync(AppDbContext db, Guid gardenId, Guid id, HarvestValues? proposed, CancellationToken token)
    {
        await WriteLocks.HarvestsAsync(db, gardenId, [id], token);
        var destinations = db.DryingItems.Where(x => x.GardenId == gardenId && x.HarvestId == id && db.DryingLots.Any(l => l.Id == x.DryingLotId && l.Validity == RecordValidity.Valid));
        var allocated = await destinations.SumAsync(x => (decimal?)x.AllocatedMassKg, token) ?? 0m;
        if (proposed is null) return allocated > 0 ? Result<Unit>.Failure(RecordRules.HasDependents("Colheita possui destinações válidas de secagem.")) : Result<Unit>.Success(new());
        var current = await db.Harvests.AsNoTracking().SingleAsync(x => x.Id == id && x.GardenId == gardenId, token);
        if (allocated > 0 && (proposed.UsableMassKg is null || proposed.UsableMassKg < allocated || current.CycleId != proposed.CycleId) ||
            await destinations.AnyAsync(x => db.DryingLots.Any(l => l.Id == x.DryingLotId && l.StartedOn < proposed.HarvestedOn), token))
            return Result<Unit>.Failure(RecordRules.HasDependents("Pesagem, ciclo ou data propostos contradizem a massa já destinada; revise os lotes."));
        return Result<Unit>.Success(new());
    }
}
