using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
namespace HortaComunitaria.Api.Features.Occurrences.OpenOccurrence;

internal sealed record OpenOccurrenceRequest([property: JsonRequired] Guid OperationId, [property: JsonRequired] DateTimeOffset OpenedAt, [property: JsonRequired] string Category, [property: JsonRequired] string Description, Guid? ResponsibleParticipantId, Guid? BedId, Guid? SectorId, Guid? CycleId, Guid? IrrigationId);
internal sealed class OpenOccurrenceCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<OpenOccurrenceCommandHandler> logger)
{
    public async Task<Result<CreatedOccurrenceResponse>> HandleAsync(Guid gardenId, OpenOccurrenceRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Occurrences.OpenOccurrence", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId });
        Result<CreatedOccurrenceResponse> Reject(Error error) => process.Complete(Result<CreatedOccurrenceResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = OccurrenceValues.Create(InstantPrecision.ForPersistence(request.OpenedAt), request.Category, request.Description, OccurrenceStatus.Open, null, null, request.ResponsibleParticipantId, request.BedId, request.SectorId, request.CycleId, request.IrrigationId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        if (request.OperationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var payload = AuditSnapshots.Capture(values);
        var existing = await db.Occurrences.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == request.OperationId, cancellationToken);
        if (existing is not null)
        {
            using var original = System.Text.Json.JsonDocument.Parse(existing.CreationPayload);
            using var candidatePayload = System.Text.Json.JsonDocument.Parse(payload);
            if (!System.Text.Json.JsonElement.DeepEquals(original.RootElement, candidatePayload.RootElement)) return Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<CreatedOccurrenceResponse>.Success(new(false, OccurrenceProjection.Map(existing))));
        }
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await OccurrencesRecordContext.ResolveOccurrenceAsync(db, gardenId, values, Guid.Empty, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var candidate = Occurrence.Create(gardenId, request.OperationId, values, await author.ResolveAsync(cancellationToken), now, payload);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        db.Occurrences.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Occurrence", entity.Id, entity.RecordedByUserId, now, null, AuditSnapshots.Capture(OccurrenceProjection.Map(entity))));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("OpenOccurrenceConfirmed", "Created", "Operação OpenOccurrence confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CreatedOccurrenceResponse>.Success(new(true, OccurrenceProjection.Map(entity))));
    }
}
