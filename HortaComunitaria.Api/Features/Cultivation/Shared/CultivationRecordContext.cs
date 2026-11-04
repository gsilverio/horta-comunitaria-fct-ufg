using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record CycleContext(Guid BedConfigurationId, decimal ReferenceAreaM2);
internal static class CultivationRecordContext
{
    internal static Task<Result<Unit>> ResolveDryingLotAsync(AppDbContext db, Guid gardenId, DryingLotValues values, Guid id, CancellationToken token) =>
        DryingContext.ResolveAsync(db, gardenId, values, id, token);
    private static async Task<bool> ParticipantAsync(AppDbContext db, Guid gardenId, Guid? id, CancellationToken token) =>
        id is null || await db.Participants.AnyAsync(x => x.Id == id && x.GardenId == gardenId && x.IsActive, token);

    internal static async Task<Result<CycleContext>> ResolveCultivationCycleAsync(AppDbContext db, Guid gardenId, CultivationCycleValues values, Guid id, CancellationToken token)
    {
        if (!await db.Beds.AnyAsync(x => x.Id == values.BedId && x.GardenId == gardenId && x.IsActive, token) ||
            !await db.Species.AnyAsync(x => x.Id == values.SpeciesId && x.IsActive, token) ||
            values.CultivarId is not null && !await db.Cultivars.AnyAsync(x => x.Id == values.CultivarId && x.SpeciesId == values.SpeciesId && x.IsActive, token) ||
            !await ParticipantAsync(db, gardenId, values.ResponsibleParticipantId, token)) return Result<CycleContext>.Failure(Error.NotFound());
        var configuration = await db.BedConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.BedId == values.BedId && x.ValidFrom <= values.StartedOn && (x.ValidUntil == null || values.StartedOn < x.ValidUntil), token);
        if (configuration is null) return Result<CycleContext>.Failure(Error.Validation("bedId", "Configure a área cultivável vigente antes de abrir o ciclo."));
        var error = CultivationRules.Area(values.OccupiedAreaM2, configuration.CultivableAreaM2);
        return error is not null ? Result<CycleContext>.Failure(error) : Result<CycleContext>.Success(new(configuration.Id, configuration.CultivableAreaM2));
    }

    internal static async Task<Result<Unit>> ResolvePlantingAsync(AppDbContext db, Guid gardenId, PlantingValues values, Guid id, CancellationToken token)
    {
        var cycle = await db.CultivationCycles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.CycleId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
        if (cycle is null || !await ParticipantAsync(db, gardenId, values.ResponsibleParticipantId, token)) return Result<Unit>.Failure(Error.NotFound());
        var error = CultivationRules.EventDate(cycle, values.PlantedOn);
        if (error is not null) return Result<Unit>.Failure(error);
        var lot = values.PropagationLotId is null ? null : await db.PropagationLots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.PropagationLotId && x.GardenId == gardenId && x.IsActive, token);
        if (values.PropagationLotId is not null && lot is null) return Result<Unit>.Failure(Error.NotFound());
        error = CultivationRules.Origin(cycle, lot);
        if (error is not null) return Result<Unit>.Failure(error);
        if (values.ReplacedPlantingId is { } replacedId)
        {
            var replaced = await db.Plantings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == replacedId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
            if (replaced is null) return Result<Unit>.Failure(Error.NotFound());
            if (replacedId == id || replaced.CycleId != values.CycleId || replaced.PlantedOn > values.PlantedOn)
                return Result<Unit>.Failure(Error.Validation("replacedPlantingId", "Reposição exige outro plantio anterior do mesmo ciclo."));
            var circular = await db.Database.SqlQuery<bool>($"""
                WITH RECURSIVE chain AS (
                  SELECT "Id", "ReplacedPlantingId", ARRAY["Id"] AS path FROM plantings
                  WHERE "GardenId" = {gardenId} AND "Id" = {replacedId}
                  UNION ALL
                  SELECT p."Id", p."ReplacedPlantingId", c.path || p."Id" FROM plantings p
                  JOIN chain c ON p."Id" = c."ReplacedPlantingId"
                  WHERE p."GardenId" = {gardenId} AND NOT p."Id" = ANY(c.path)
                )
                SELECT EXISTS(SELECT 1 FROM chain WHERE "Id" = {id} OR "ReplacedPlantingId" = ANY(path)) AS "Value"
                """).SingleAsync(token);
            if (circular) return Result<Unit>.Failure(Error.Validation("replacedPlantingId", "Cadeia de reposições circular."));
        }
        return Result<Unit>.Success(new());
    }

    internal static async Task<Result<Unit>> ResolveEstablishmentAssessmentAsync(AppDbContext db, Guid gardenId, EstablishmentAssessmentValues values, Guid id, CancellationToken token)
    {
        var planting = await db.Plantings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.PlantingId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
        if (planting is null || !await ParticipantAsync(db, gardenId, values.ResponsibleParticipantId, token)) return Result<Unit>.Failure(Error.NotFound());
        var error = CultivationRules.Assessment(planting, values);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }

    internal static async Task<Result<Unit>> ResolveAgronomicObservationAsync(AppDbContext db, Guid gardenId, AgronomicObservationValues values, Guid id, CancellationToken token)
    {
        var cycle = await db.CultivationCycles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.CycleId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
        if (cycle is null || !await ParticipantAsync(db, gardenId, values.ResponsibleParticipantId, token)) return Result<Unit>.Failure(Error.NotFound());
        var error = CultivationRules.EventDate(cycle, values.ObservedOn);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }

    internal static async Task<Result<Unit>> ResolveHarvestAsync(AppDbContext db, Guid gardenId, HarvestValues values, Guid id, CancellationToken token)
    {
        var cycle = await db.CultivationCycles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.CycleId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token);
        if (cycle is null || !await ParticipantAsync(db, gardenId, values.ResponsibleParticipantId, token)) return Result<Unit>.Failure(Error.NotFound());
        var error = CultivationRules.EventDate(cycle, values.HarvestedOn);
        return error is null ? Result<Unit>.Success(new()) : Result<Unit>.Failure(error);
    }
}
