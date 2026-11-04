using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.RegisterAgronomicObservation;

internal sealed record RegisterAgronomicObservationRequest([property: JsonRequired] Guid OperationId, [property: JsonRequired] Guid CycleId, [property: JsonRequired] DateOnly ObservedOn, [property: JsonRequired] string Category, [property: JsonRequired] string Description, decimal? Measurement, string? Unit, string? Method, Guid? ResponsibleParticipantId);
internal sealed class RegisterAgronomicObservationCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<RegisterAgronomicObservationCommandHandler> logger)
{
    public async Task<Result<CreatedAgronomicObservationResponse>> HandleAsync(Guid gardenId, RegisterAgronomicObservationRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.RegisterAgronomicObservation", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId });
        Result<CreatedAgronomicObservationResponse> Reject(Error error) => process.Complete(Result<CreatedAgronomicObservationResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = AgronomicObservationValues.Create(request.CycleId, request.ObservedOn, request.Category, request.Description, request.Measurement, request.Unit, request.Method, request.ResponsibleParticipantId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        if (request.OperationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var payload = AuditSnapshots.Capture(values);
        var existing = await db.AgronomicObservations.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == request.OperationId, cancellationToken);
        if (existing is not null)
        {
            using var original = System.Text.Json.JsonDocument.Parse(existing.CreationPayload);
            using var candidatePayload = System.Text.Json.JsonDocument.Parse(payload);
            if (!System.Text.Json.JsonElement.DeepEquals(original.RootElement, candidatePayload.RootElement)) return Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<CreatedAgronomicObservationResponse>.Success(new(false, AgronomicObservationProjection.Map(existing))));
        }
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CultivationRecordContext.ResolveAgronomicObservationAsync(db, gardenId, values, Guid.Empty, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var candidate = AgronomicObservation.Create(gardenId, request.OperationId, values, await author.ResolveAsync(cancellationToken), now, payload);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        db.AgronomicObservations.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "AgronomicObservation", entity.Id, entity.RecordedByUserId, now, null, AuditSnapshots.Capture(AgronomicObservationProjection.Map(entity))));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("RegisterAgronomicObservationConfirmed", "Created", "Operação RegisterAgronomicObservation confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CreatedAgronomicObservationResponse>.Success(new(true, AgronomicObservationProjection.Map(entity))));
    }
}
