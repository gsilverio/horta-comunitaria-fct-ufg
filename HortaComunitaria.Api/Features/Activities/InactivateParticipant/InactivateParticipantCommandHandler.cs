using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HortaComunitaria.Api.Features.Activities.InactivateParticipant;

internal sealed record InactivateParticipantRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InactivateParticipantCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<InactivateParticipantCommandHandler> logger)
{
    public async Task<Result<ParticipantResponse>> HandleAsync(Guid gardenId, Guid id, InactivateParticipantRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.InactivateParticipant", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<ParticipantResponse> Reject(Error error) => process.Complete(Result<ParticipantResponse>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        process.Step("LoadingRecord", "Carregando cadastro e conferindo a versão esperada");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var entity = await db.Participants.SingleOrDefaultAsync(x => x.Id == id && x.GardenId == gardenId, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var before = AuditSnapshots.Capture(ParticipantProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Inactivate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            if (await db.Participants.AnyAsync(x => false, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Participant", id, await author.ResolveAsync(cancellationToken), now, before,
                AuditSnapshots.Capture(ParticipantProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando alteração e histórico sem excluir o cadastro");
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { return Reject(new("record.version_conflict", "Cadastro alterado; recarregue e revise.", ErrorCategory.Conflict)); }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("InactivateParticipantConfirmed", "Invalidated", "Operação InactivateParticipant confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<ParticipantResponse>.Success(ParticipantProjection.Map(entity)));
    }
}
