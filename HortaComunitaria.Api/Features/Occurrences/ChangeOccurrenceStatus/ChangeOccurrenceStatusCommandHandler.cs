using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
namespace HortaComunitaria.Api.Features.Occurrences.ChangeOccurrenceStatus;

internal sealed record ChangeOccurrenceStatusRequest([property: JsonRequired] OccurrenceStatus Status, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class ChangeOccurrenceStatusCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<ChangeOccurrenceStatusCommandHandler> logger)
{
    public async Task<Result<OccurrenceResponse>> HandleAsync(Guid gardenId, Guid id, ChangeOccurrenceStatusRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Occurrences.ChangeOccurrenceStatus", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<OccurrenceResponse> Reject(Error error) => process.Complete(Result<OccurrenceResponse>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("LoadingRecord", "Conferindo estado, versão e dependentes do registro");
        var entity = await db.Occurrences.SingleOrDefaultAsync(x => x.Id == id && x.GardenId == gardenId, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if (request.Status is not (OccurrenceStatus.Open or OccurrenceStatus.InProgress)) return Reject(Error.Validation("status", "Mudança simples aceita Open ou InProgress; use a resolução para concluir."));
        var candidate = OccurrenceValues.Create(entity.OpenedAt, entity.Category, entity.Description, request.Status, entity.ResolvedAt, entity.Resolution, entity.ResponsibleParticipantId, entity.BedId, entity.SectorId, entity.CycleId, entity.IrrigationId);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var values = candidate.Value;
        var dependencies = await OccurrencesDependencies.CheckOccurrenceAsync(db, gardenId, id, values, cancellationToken);
        if (!dependencies.IsSuccess) return Reject(dependencies.Error);
        var context = await OccurrencesRecordContext.ResolveOccurrenceAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var before = AuditSnapshots.Capture(OccurrenceProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Occurrence", id, await author.ResolveAsync(cancellationToken), now,
                before, AuditSnapshots.Capture(OccurrenceProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando novo estado e histórico da alteração");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("ChangeOccurrenceStatusConfirmed", "Updated", "Operação ChangeOccurrenceStatus confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<OccurrenceResponse>.Success(OccurrenceProjection.Map(entity)));
    }
}
