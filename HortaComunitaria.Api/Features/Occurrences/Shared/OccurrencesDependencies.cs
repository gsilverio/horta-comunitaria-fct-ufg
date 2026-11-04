using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Occurrences.Shared;

internal static class OccurrencesDependencies
{
    internal static async Task<Result<Unit>> CheckOccurrenceAsync(AppDbContext db, Guid gardenId, Guid id, OccurrenceValues? proposed, CancellationToken token)
    {
        if (!await db.Activities.AnyAsync(x => x.GardenId == gardenId && x.OccurrenceId == id && x.Validity == RecordValidity.Valid, token)) return Result<Unit>.Success(new());
        var current = await db.Occurrences.AsNoTracking().SingleAsync(x => x.Id == id && x.GardenId == gardenId, token);
        if (proposed is null || proposed.BedId != current.BedId || proposed.SectorId != current.SectorId || proposed.CycleId != current.CycleId || proposed.IrrigationId != current.IrrigationId)
            return Result<Unit>.Failure(RecordRules.HasDependents("Ocorrência possui atividades válidas; revise-as antes de invalidar ou trocar o contexto."));
        return Result<Unit>.Success(new());
    }
}
