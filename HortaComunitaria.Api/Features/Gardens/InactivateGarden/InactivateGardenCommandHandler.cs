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

namespace HortaComunitaria.Api.Features.Gardens.InactivateGarden;

internal sealed record InactivateGardenRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InactivateGardenCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InactivateGardenCommandHandler> logger)
{
    public async Task<Result<GardenResponse>> HandleAsync(Guid id, InactivateGardenRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.InactivateGarden", new Dictionary<string, object?> { ["ResourceId"] = id });
        Result<GardenResponse> Reject(Error error) => process.Complete(Result<GardenResponse>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        process.Step("LoadingRecord", "Carregando cadastro e conferindo a versão esperada");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, id, cancellationToken);

        var entity = await db.Gardens.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var before = AuditSnapshots.Capture(GardenProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Inactivate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            if (await db.Gardens.AnyAsync(x => x.Code == entity.Code && x.Id != id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(id, "Garden", id, await author.ResolveAsync(cancellationToken), now, before,
                AuditSnapshots.Capture(GardenProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando alteração e histórico sem excluir o cadastro");
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { return Reject(new("record.version_conflict", "Cadastro alterado; recarregue e revise.", ErrorCategory.Conflict)); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("InactivateGardenConfirmed", "Invalidated", "Operação InactivateGarden confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<GardenResponse>.Success(GardenProjection.Map(entity)));
    }
}
