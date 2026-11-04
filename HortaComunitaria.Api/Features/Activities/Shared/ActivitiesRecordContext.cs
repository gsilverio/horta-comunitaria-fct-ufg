using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Activities.Shared;

internal static class ActivitiesRecordContext
{
    internal static async Task<Result<Unit>> ResolveActivityAsync(AppDbContext db, Guid gardenId, ActivityValues values, Guid id, CancellationToken token)
    {
        if (values.BedId is not null && !await db.Beds.AnyAsync(x => x.Id == values.BedId && x.GardenId == gardenId && x.IsActive, token) ||
            values.SectorId is not null && !await db.IrrigationSectors.AnyAsync(x => x.Id == values.SectorId && x.GardenId == gardenId && x.IsActive, token)) return Result<Unit>.Failure(Error.NotFound());
        Guid? bedId = values.BedId;
        if (values.CycleId is { } cycleId)
        {
            var cycle = await db.CultivationCycles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == cycleId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
            if (cycle is null) return Result<Unit>.Failure(Error.NotFound());
            bedId = cycle.BedId;
        }
        var people = values.Participants.Select(x => x.ParticipantId).ToArray();
        var institutions = values.Institutions.Select(x => x.InstitutionId).ToArray();
        var inputIds = values.InputUsages.Select(x => x.InputId).Distinct().ToArray();
        if (await db.Participants.CountAsync(x => people.Contains(x.Id) && x.GardenId == gardenId && x.IsActive, token) != people.Length ||
            await db.Institutions.CountAsync(x => institutions.Contains(x.Id) && x.IsActive, token) != institutions.Length) return Result<Unit>.Failure(Error.NotFound());
        var inputs = await db.Inputs.AsNoTracking().Where(x => inputIds.Contains(x.Id) && x.IsActive).Select(x => new { x.Id, x.ReferenceUnit }).ToDictionaryAsync(x => x.Id, token);
        if (inputs.Count != inputIds.Length) return Result<Unit>.Failure(Error.NotFound());
        if (values.InputUsages.Any(x => inputs[x.InputId].ReferenceUnit != x.Unit)) return Result<Unit>.Failure(Error.Validation("unit", "Uso de insumo exige a unidade cadastrada; não há conversão implícita."));
        if (values.OccurrenceId is { } occurrenceId)
        {
            var occurrence = await db.Occurrences.AsNoTracking().SingleOrDefaultAsync(x => x.Id == occurrenceId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
            if (occurrence is null) return Result<Unit>.Failure(Error.NotFound());
            Guid? occurrenceBed = occurrence.BedId;
            Guid? occurrenceSector = occurrence.SectorId;
            if (occurrence.CycleId is { } sourceCycle)
                occurrenceBed = await db.CultivationCycles.AsNoTracking().Where(x => x.Id == sourceCycle && x.GardenId == gardenId).Select(x => (Guid?)x.BedId).SingleAsync(token);
            if (occurrence.IrrigationId is { } sourceIrrigation)
                occurrenceSector = await db.Irrigations.AsNoTracking().Where(x => x.Id == sourceIrrigation && x.GardenId == gardenId).Select(x => (Guid?)x.IrrigationSectorId).SingleAsync(token);
            Guid? activitySector = values.SectorId;
            if (bedId is { } bed)
                activitySector = await db.BedSectorAssignments.AsNoTracking().Where(x => x.GardenId == gardenId && x.BedId == bed && x.ValidFrom <= values.PerformedOn && (x.ValidUntil == null || values.PerformedOn < x.ValidUntil)).Select(x => (Guid?)x.IrrigationSectorId).SingleOrDefaultAsync(token);
            var occurrenceBedBelongsToActivitySector = occurrenceBed is null || values.SectorId is null ||
                await db.BedSectorAssignments.AnyAsync(x => x.GardenId == gardenId && x.BedId == occurrenceBed && x.IrrigationSectorId == values.SectorId && x.ValidFrom <= values.PerformedOn && (x.ValidUntil == null || values.PerformedOn < x.ValidUntil), token);
            var compatibility = ActivityRules.OccurrenceContext(bedId, activitySector, values.CycleId,
                occurrenceBed, occurrenceSector, occurrence.CycleId, occurrenceBedBelongsToActivitySector);
            if (compatibility is not null) return Result<Unit>.Failure(compatibility);
        }
        return Result<Unit>.Success(new());
    }
}
