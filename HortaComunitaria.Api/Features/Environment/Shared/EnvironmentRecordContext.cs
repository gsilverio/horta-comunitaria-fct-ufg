using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Environment.Shared;

internal sealed record MeasurementContext(Guid? BedId, Guid? SectorId, Guid? BedSectorAssignmentId);
internal static class EnvironmentRecordContext
{
    internal static async Task<Result<MeasurementContext>> ResolveEnvironmentalMeasurementAsync(AppDbContext db, Guid gardenId,
        EnvironmentalMeasurementValues values, Guid id, CancellationToken cancellationToken)
    {
        var point = await db.SamplingPoints.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.SamplingPointId && x.GardenId == gardenId && x.IsActive, cancellationToken);
        var variable = await db.EnvironmentalVariables.AsNoTracking().SingleOrDefaultAsync(x => x.Id == values.VariableId && x.IsActive, cancellationToken);
        if (point is null || variable is null || values.ResponsibleParticipantId is not null &&
            !await db.Participants.AnyAsync(x => x.Id == values.ResponsibleParticipantId && x.GardenId == gardenId && x.IsActive, cancellationToken))
            return Result<MeasurementContext>.Failure(Error.NotFound());
        if (variable.ReferenceUnit != values.Unit) return Result<MeasurementContext>.Failure(Error.Validation("unit", "Unidade deve corresponder à grandeza cadastrada; não há conversão implícita."));
        if (point.BedId is not null && !await db.Beds.AnyAsync(x => x.Id == point.BedId && x.GardenId == gardenId && x.IsActive, cancellationToken) ||
            point.SectorId is not null && !await db.IrrigationSectors.AnyAsync(x => x.Id == point.SectorId && x.GardenId == gardenId && x.IsActive, cancellationToken))
            return Result<MeasurementContext>.Failure(Error.NotFound());
        var on = LocalPeriod.LocalDate(values.CollectedAt);
        var assignment = point.BedId is null ? null : await db.BedSectorAssignments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.GardenId == gardenId && x.BedId == point.BedId && x.ValidFrom <= on && (x.ValidUntil == null || on < x.ValidUntil), cancellationToken);
        return Result<MeasurementContext>.Success(new(point.BedId, point.SectorId ?? assignment?.IrrigationSectorId, assignment?.Id));
    }
}
