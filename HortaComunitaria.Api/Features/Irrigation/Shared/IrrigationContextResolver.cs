using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Irrigation.Shared;

internal static class IrrigationContextResolver
{
    internal static Result<Unit> ValidateIds(Guid gardenId, Guid sectorId, Guid[]? bedIds, Guid? responsibleId)
    {
        if (gardenId == Guid.Empty || sectorId == Guid.Empty || responsibleId == Guid.Empty)
            return Result<Unit>.Failure(Error.Validation("id", "Identificadores devem ser UUIDs não vazios."));
        if (bedIds is null || bedIds.Any(x => x == Guid.Empty) || bedIds.Distinct().Count() != bedIds.Length)
            return Result<Unit>.Failure(Error.Validation("bedIds", "Informe a lista de canteiros, sem identificadores vazios ou repetidos."));
        return Result<Unit>.Success(new());
    }

    internal static async Task<Result<IReadOnlyCollection<BedSectorAssignment>>> ResolveAsync(AppDbContext db,
        Guid gardenId, Guid sectorId, Guid[] bedIds, Guid? responsibleId, DateTimeOffset startedAt, CancellationToken cancellationToken)
    {
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken) ||
            !await db.IrrigationSectors.AsNoTracking().AnyAsync(x => x.Id == sectorId && x.GardenId == gardenId && x.IsActive, cancellationToken))
            return Result<IReadOnlyCollection<BedSectorAssignment>>.Failure(Error.NotFound());
        if (responsibleId is not null && !await db.Participants.AsNoTracking().AnyAsync(
            x => x.Id == responsibleId && x.GardenId == gardenId && x.IsActive, cancellationToken))
            return Result<IReadOnlyCollection<BedSectorAssignment>>.Failure(Error.NotFound());
        var selected = await db.Beds.AsNoTracking().Where(x => bedIds.Contains(x.Id) && x.GardenId == gardenId && x.IsActive)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (selected.Count != bedIds.Length) return Result<IReadOnlyCollection<BedSectorAssignment>>.Failure(Error.NotFound());
        var on = LocalPeriod.LocalDate(startedAt);
        var assignments = await db.BedSectorAssignments.AsNoTracking().Where(x => x.GardenId == gardenId &&
            bedIds.Contains(x.BedId) && x.ValidFrom <= on && (x.ValidUntil == null || on < x.ValidUntil)).ToListAsync(cancellationToken);
        if (assignments.Count != bedIds.Length || assignments.Select(x => x.BedId).Distinct().Count() != bedIds.Length ||
            assignments.Any(x => x.IrrigationSectorId != sectorId))
            return Result<IReadOnlyCollection<BedSectorAssignment>>.Failure(new("irrigation.invalid_bed_assignment",
                "Cada canteiro exige um único vínculo vigente com o setor informado.", ErrorCategory.Validation,
                new Dictionary<string, string[]> { ["bedIds"] = ["Revise setor, data e vínculos dos canteiros."] }));
        return Result<IReadOnlyCollection<BedSectorAssignment>>.Success(assignments);
    }
}
