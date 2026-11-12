using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.AssignBedToSector;

internal sealed record AssignBedToSectorRequest([property: JsonRequired] Guid SectorId, [property: JsonRequired] DateOnly ValidFrom, [property: JsonRequired] int ExpectedBedVersion, string? Reason);
internal sealed record AssignBedToSectorResponse(BedSectorAssignmentResponse Context, int BedVersion);
internal sealed class AssignBedToSectorCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<AssignBedToSectorCommandHandler> logger)
{
    public async Task<Result<AssignBedToSectorResponse>> HandleAsync(Guid gardenId, Guid bedId, AssignBedToSectorRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.AssignBedToSector", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["BedId"] = bedId });
        Result<AssignBedToSectorResponse> Reject(Error error) => process.Complete(Result<AssignBedToSectorResponse>.Failure(error));
        if (gardenId == Guid.Empty || bedId == Guid.Empty || request.ValidFrom == default) return Reject(Error.Validation("id", "Informe referências e início válidos."));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        await WriteLocks.BedsAsync(db, gardenId, [bedId], cancellationToken);
        process.Step("CheckingContext", "Conferindo canteiro e vigências sob proteção transacional");
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var bed = await db.Beds.SingleOrDefaultAsync(x => x.Id == bedId && x.GardenId == gardenId && x.IsActive, cancellationToken);
        if (bed is null) return Reject(Error.NotFound());
        var existing = await db.BedSectorAssignments.Where(x => x.BedId == bedId).OrderBy(x => x.ValidFrom).ToListAsync(cancellationToken);
        var reason = ChangeReason.Create(existing.Count == 0 ? request.Reason ?? "Configuração inicial" : request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var today = LocalPeriod.LocalDate(clock.GetUtcNow());
        if (existing.Count > 0 && (request.ValidFrom < today || request.ValidFrom <= existing[^1].ValidFrom || existing[^1].ValidUntil is not null))
            return Reject(Error.Validation("validFrom", "Sucessão deve começar hoje ou depois, após a última vigência aberta; correção retroativa exige revisão específica."));
        var before = AuditSnapshots.Capture(new { Bed = BedProjection.Map(bed), Contexts = existing.Select(BedSectorAssignmentProjection.Map).ToArray() });

        var sector = await db.IrrigationSectors.SingleOrDefaultAsync(x => x.Id == request.SectorId && x.GardenId == gardenId && x.IsActive, cancellationToken);
        if (sector is null) return Reject(Error.NotFound());
        var range = LocalPeriod.Create(request.ValidFrom.ToString("yyyy-MM-dd"), request.ValidFrom.ToString("yyyy-MM-dd"));
        if (!range.IsSuccess) return Reject(range.Error);
        if (await db.Irrigations.AnyAsync(x => x.GardenId == gardenId && x.StartedAt >= range.Value.StartUtc &&
            db.IrrigationBeds.Any(b => b.IrrigationId == x.Id && b.BedId == bedId), cancellationToken))
            return Reject(RecordRules.HasDependents("Há irrigações no período afetado; a mudança não pode reclassificar seu contexto histórico."));

        if (await db.EnvironmentalMeasurements.AnyAsync(x => x.GardenId == gardenId && x.BedId == bedId && x.CollectedAt >= range.Value.StartUtc, cancellationToken))
            return Reject(RecordRules.HasDependents("Há coletas no período afetado; a mudança não pode reclassificar seu contexto histórico."));
        if (existing.Count > 0)
        {
            var closed = existing[^1].Close(request.ValidFrom);
            if (!closed.IsSuccess) return Reject(closed.Error);
        }
        var candidate = BedSectorAssignment.Create(bed, sector, request.ValidFrom, null, existing);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = bed.ChangeContext(request.ExpectedBedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        db.BedSectorAssignments.Add(candidate.Value);
        var after = AuditSnapshots.Capture(new { Bed = BedProjection.Map(bed), Contexts = existing.Append(candidate.Value).Select(BedSectorAssignmentProjection.Map).ToArray() });
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Bed", bedId, await author.ResolveAsync(cancellationToken), now, before, after, reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando sucessão de vigência, versão do canteiro e histórico");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<AssignBedToSectorResponse>.Success(new(BedSectorAssignmentProjection.Map(candidate.Value), bed.Version)));
    }
}
