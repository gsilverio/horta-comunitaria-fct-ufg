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
namespace HortaComunitaria.Api.Features.Cultivation.InvalidatePlanting;

internal sealed record InvalidatePlantingRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InvalidatePlantingCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InvalidatePlantingCommandHandler> logger)
{
    public async Task<Result<PlantingResponse>> HandleAsync(Guid gardenId, Guid id, InvalidatePlantingRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.InvalidatePlanting", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<PlantingResponse> Reject(Error error) => process.Complete(Result<PlantingResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        PlantingValues? values = null;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.Plantings.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(PlantingProjection.Map(entity));
        var dependencies = await CultivationDependencies.CheckPlantingAsync(db, gardenId, id, values, cancellationToken);
        if (!dependencies.IsSuccess) return Reject(dependencies.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Invalidate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Planting", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(PlantingProjection.Map(entity)), reason.Value.Value, AuditOperation.Invalidated));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("InvalidatePlantingConfirmed", "Invalidated", "Operação InvalidatePlanting confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<PlantingResponse>.Success(PlantingProjection.Map(entity)));
    }
}
