using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HortaComunitaria.Api.Features.Gardens.InactivateIrrigationSector;

internal sealed record InactivateIrrigationSectorRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InactivateIrrigationSectorCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InactivateIrrigationSectorCommandHandler> logger)
{
    public async Task<Result<IrrigationSectorResponse>> HandleAsync(Guid gardenId, Guid id, InactivateIrrigationSectorRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.InactivateIrrigationSector", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<IrrigationSectorResponse> Reject(Error error) => process.Complete(Result<IrrigationSectorResponse>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        process.Step("LoadingRecord", "Carregando cadastro e conferindo a versão esperada");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var entity = await db.IrrigationSectors.SingleOrDefaultAsync(x => x.Id == id && x.GardenId == gardenId, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var before = AuditSnapshots.Capture(IrrigationSectorProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Inactivate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            if (await db.IrrigationSectors.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code && x.Id != id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "IrrigationSector", id, await author.ResolveAsync(cancellationToken), now, before,
                AuditSnapshots.Capture(IrrigationSectorProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando alteração e histórico sem excluir o cadastro");
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { return Reject(new("record.version_conflict", "Cadastro alterado; recarregue e revise.", ErrorCategory.Conflict)); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("InactivateIrrigationSectorConfirmed", "Invalidated", "Operação InactivateIrrigationSector confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<IrrigationSectorResponse>.Success(IrrigationSectorProjection.Map(entity)));
    }
}
