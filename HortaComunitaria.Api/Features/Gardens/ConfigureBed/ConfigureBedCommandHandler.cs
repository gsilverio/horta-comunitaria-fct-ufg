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

namespace HortaComunitaria.Api.Features.Gardens.ConfigureBed;

internal sealed record ConfigureBedRequest([property: JsonRequired] decimal CultivableAreaM2, decimal? LengthMeters, decimal? WidthMeters, [property: JsonRequired] DateOnly ValidFrom, [property: JsonRequired] int ExpectedBedVersion, string? Reason);
internal sealed record ConfigureBedResponse(BedConfigurationResponse Context, int BedVersion);
internal sealed class ConfigureBedCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<ConfigureBedCommandHandler> logger)
{
    public async Task<Result<ConfigureBedResponse>> HandleAsync(Guid gardenId, Guid bedId, ConfigureBedRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ConfigureBed", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["BedId"] = bedId });
        Result<ConfigureBedResponse> Reject(Error error) => process.Complete(Result<ConfigureBedResponse>.Failure(error));
        if (gardenId == Guid.Empty || bedId == Guid.Empty || request.ValidFrom == default) return Reject(Error.Validation("id", "Informe referências e início válidos."));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        await WriteLocks.BedsAsync(db, gardenId, [bedId], cancellationToken);
        process.Step("CheckingContext", "Conferindo canteiro e vigências sob proteção transacional");
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var bed = await db.Beds.SingleOrDefaultAsync(x => x.Id == bedId && x.GardenId == gardenId && x.IsActive, cancellationToken);
        if (bed is null) return Reject(Error.NotFound());
        var existing = await db.BedConfigurations.Where(x => x.BedId == bedId).OrderBy(x => x.ValidFrom).ToListAsync(cancellationToken);
        var reason = ChangeReason.Create(existing.Count == 0 ? request.Reason ?? "Configuração inicial" : request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var today = LocalPeriod.LocalDate(clock.GetUtcNow());
        if (existing.Count > 0 && (request.ValidFrom < today || request.ValidFrom <= existing[^1].ValidFrom || existing[^1].ValidUntil is not null))
            return Reject(Error.Validation("validFrom", "Sucessão deve começar hoje ou depois, após a última vigência aberta; correção retroativa exige revisão específica."));
        if (await db.CultivationCycles.AnyAsync(x => x.GardenId == gardenId && x.BedId == bedId && x.StartedOn >= request.ValidFrom, cancellationToken))
            return Reject(RecordRules.HasDependents("Há ciclos com referência de área no período afetado; revise seus contextos antes da sucessão."));
        var before = AuditSnapshots.Capture(new { Bed = BedProjection.Map(bed), Contexts = existing.Select(BedConfigurationProjection.Map).ToArray() });

        if (existing.Count > 0)
        {
            var closed = existing[^1].Close(request.ValidFrom);
            if (!closed.IsSuccess) return Reject(closed.Error);
        }
        var candidate = BedConfiguration.Create(gardenId, bedId, request.ValidFrom, request.LengthMeters, request.WidthMeters, request.CultivableAreaM2);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = bed.ChangeContext(request.ExpectedBedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        db.BedConfigurations.Add(candidate.Value);
        var after = AuditSnapshots.Capture(new { Bed = BedProjection.Map(bed), Contexts = existing.Append(candidate.Value).Select(BedConfigurationProjection.Map).ToArray() });
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Bed", bedId, await author.ResolveAsync(cancellationToken), now, before, after, reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando sucessão de vigência, versão do canteiro e histórico");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<ConfigureBedResponse>.Success(new(BedConfigurationProjection.Map(candidate.Value), bed.Version)));
    }
}
