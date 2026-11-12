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
namespace HortaComunitaria.Api.Features.Occurrences.CorrectOccurrence;

internal sealed record CorrectOccurrenceRequest([property: JsonRequired] DateTimeOffset OpenedAt, [property: JsonRequired] string Category, [property: JsonRequired] string Description, [property: JsonRequired] OccurrenceStatus Status, [property: JsonRequired] DateTimeOffset? ResolvedAt, [property: JsonRequired] string? Resolution, [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] Guid? BedId, [property: JsonRequired] Guid? SectorId, [property: JsonRequired] Guid? CycleId, [property: JsonRequired] Guid? IrrigationId, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectOccurrenceCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CorrectOccurrenceCommandHandler> logger)
{
    public async Task<Result<OccurrenceResponse>> HandleAsync(Guid gardenId, Guid id, CorrectOccurrenceRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Occurrences.CorrectOccurrence", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<OccurrenceResponse> Reject(Error error) => process.Complete(Result<OccurrenceResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = OccurrenceValues.Create(InstantPrecision.ForPersistence(request.OpenedAt), request.Category, request.Description, request.Status, InstantPrecision.ForPersistence(request.ResolvedAt), request.Resolution, request.ResponsibleParticipantId, request.BedId, request.SectorId, request.CycleId, request.IrrigationId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.Occurrences.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(OccurrenceProjection.Map(entity));
        var dependencies = await OccurrencesDependencies.CheckOccurrenceAsync(db, gardenId, id, values, cancellationToken);
        if (!dependencies.IsSuccess) return Reject(dependencies.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await OccurrencesRecordContext.ResolveOccurrenceAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<OccurrenceResponse>.Success(OccurrenceProjection.Map(entity)));
        }
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Occurrence", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(OccurrenceProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectOccurrenceConfirmed", "Updated", "Operação CorrectOccurrence confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<OccurrenceResponse>.Success(OccurrenceProjection.Map(entity)));
    }
}
