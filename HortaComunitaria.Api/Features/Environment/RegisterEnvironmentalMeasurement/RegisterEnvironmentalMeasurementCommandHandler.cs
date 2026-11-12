using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.RegisterEnvironmentalMeasurement;

internal sealed record RegisterEnvironmentalMeasurementRequest([property: JsonRequired] Guid OperationId, [property: JsonRequired] Guid SamplingPointId, [property: JsonRequired] Guid VariableId, [property: JsonRequired] DateTimeOffset CollectedAt, [property: JsonRequired] decimal Value, [property: JsonRequired] string Unit, [property: JsonRequired] string Method, [property: JsonRequired] MeasurementQuality Quality, Guid? ResponsibleParticipantId, string? Notes);
internal sealed class RegisterEnvironmentalMeasurementCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<RegisterEnvironmentalMeasurementCommandHandler> logger)
{
    public async Task<Result<CreatedEnvironmentalMeasurementResponse>> HandleAsync(Guid gardenId, RegisterEnvironmentalMeasurementRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.RegisterEnvironmentalMeasurement", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId });
        Result<CreatedEnvironmentalMeasurementResponse> Reject(Error error) => process.Complete(Result<CreatedEnvironmentalMeasurementResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = EnvironmentalMeasurementValues.Create(request.SamplingPointId, request.VariableId, InstantPrecision.ForPersistence(request.CollectedAt), request.Value, request.Unit, request.Method, request.Quality, request.ResponsibleParticipantId, request.Notes);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        if (request.OperationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var payload = AuditSnapshots.Capture(values);
        var existing = await db.EnvironmentalMeasurements.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == request.OperationId, cancellationToken);
        if (existing is not null)
        {
            using var original = System.Text.Json.JsonDocument.Parse(existing.CreationPayload);
            using var candidatePayload = System.Text.Json.JsonDocument.Parse(payload);
            if (!System.Text.Json.JsonElement.DeepEquals(original.RootElement, candidatePayload.RootElement)) return Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<CreatedEnvironmentalMeasurementResponse>.Success(new(false, EnvironmentalMeasurementProjection.Map(existing))));
        }
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await EnvironmentRecordContext.ResolveEnvironmentalMeasurementAsync(db, gardenId, values, Guid.Empty, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var candidate = EnvironmentalMeasurement.Create(gardenId, request.OperationId, values, await author.ResolveAsync(cancellationToken), now, payload, context.Value.BedId, context.Value.SectorId, context.Value.BedSectorAssignmentId);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        db.EnvironmentalMeasurements.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "EnvironmentalMeasurement", entity.Id, entity.RecordedByUserId, now, null, AuditSnapshots.Capture(EnvironmentalMeasurementProjection.Map(entity))));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("RegisterEnvironmentalMeasurementConfirmed", "Created", "Operação RegisterEnvironmentalMeasurement confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CreatedEnvironmentalMeasurementResponse>.Success(new(true, EnvironmentalMeasurementProjection.Map(entity))));
    }
}
