using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Occurrences.Shared;

internal static class OccurrencesRecordContext
{
    internal static async Task<Result<Unit>> ResolveOccurrenceAsync(AppDbContext db, Guid gardenId, OccurrenceValues values, Guid id, CancellationToken token)
    {
        if (values.ResponsibleParticipantId is not null && !await db.Participants.AnyAsync(x => x.Id == values.ResponsibleParticipantId && x.GardenId == gardenId && x.IsActive, token) ||
            values.BedId is not null && !await db.Beds.AnyAsync(x => x.Id == values.BedId && x.GardenId == gardenId && x.IsActive, token) ||
            values.SectorId is not null && !await db.IrrigationSectors.AnyAsync(x => x.Id == values.SectorId && x.GardenId == gardenId && x.IsActive, token) ||
            values.CycleId is not null && !await db.CultivationCycles.AnyAsync(x => x.Id == values.CycleId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token) ||
            values.IrrigationId is not null && !await db.Irrigations.AnyAsync(x => x.Id == values.IrrigationId && x.GardenId == gardenId && x.Validity == RecordValidity.Valid, token))
            return Result<Unit>.Failure(Error.NotFound());
        return Result<Unit>.Success(new());
    }
}
