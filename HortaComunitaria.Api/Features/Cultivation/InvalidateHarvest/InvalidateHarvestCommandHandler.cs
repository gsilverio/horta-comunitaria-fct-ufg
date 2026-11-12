using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.InvalidateHarvest;

internal sealed record InvalidateHarvestRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InvalidateHarvestCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InvalidateHarvestCommandHandler> logger)
{
    public async Task<Result<HarvestResponse>> HandleAsync(Guid gardenId, Guid id, InvalidateHarvestRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.InvalidateHarvest", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<HarvestResponse> Reject(Error error) => process.Complete(Result<HarvestResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        HarvestValues? values = null;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.Harvests.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(HarvestProjection.Map(entity));
        var dependencies = await CultivationDependencies.CheckHarvestAsync(db, gardenId, id, values, cancellationToken);
        if (!dependencies.IsSuccess) return Reject(dependencies.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Invalidate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Harvest", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(HarvestProjection.Map(entity)), reason.Value.Value, AuditOperation.Invalidated));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("InvalidateHarvestConfirmed", "Invalidated", "Operação InvalidateHarvest confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<HarvestResponse>.Success(HarvestProjection.Map(entity)));
    }
}
