using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.ConfigureCultivarVisualGrowth;

internal sealed record ConfigureCultivarVisualGrowthRequest([property: JsonRequired] int? VisualGrowthDays, [property: JsonRequired] int? VisualHarvestDays,
    [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class ConfigureCultivarVisualGrowthCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<ConfigureCultivarVisualGrowthCommandHandler> logger)
{
    public async Task<Result<CultivarResponse>> HandleAsync(Guid speciesId, Guid id, ConfigureCultivarVisualGrowthRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ConfigureCultivarVisualGrowth");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        process.Step("CheckingCatalog", "Conferindo cadastro, versão e prazos da ilustração");
        var entity = await db.Cultivars.SingleOrDefaultAsync(x => x.SpeciesId == speciesId && x.Id == id, cancellationToken);
        if (entity is null) return process.Complete(Result<CultivarResponse>.Failure(Error.NotFound()));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return process.Complete(Result<CultivarResponse>.Failure(reason.Error));
        var before = AuditSnapshots.Capture(CultivarProjection.Map(entity));
        var at = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var result = entity.ConfigureVisualGrowth(request.ExpectedVersion, reason.Value, at, request.VisualGrowthDays, request.VisualHarvestDays);
        if (!result.IsSuccess) return process.Complete(Result<CultivarResponse>.Failure(result.Error));
        if (result.Value)
        {
            db.AuditEntries.Add(AuditSnapshots.Create(null, "Cultivar", entity.Id, await author.ResolveAsync(cancellationToken), at,
                before, AuditSnapshots.Capture(CultivarProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Salvando configuração visual e histórico");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<CultivarResponse>.Success(CultivarProjection.Map(entity)));
    }
}
