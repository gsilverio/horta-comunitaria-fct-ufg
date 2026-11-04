using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.OpenCultivationCycle;

internal sealed record OpenCultivationCycleRequest([property: JsonRequired] Guid OperationId, [property: JsonRequired] Guid BedId, [property: JsonRequired] Guid SpeciesId, Guid? CultivarId, [property: JsonRequired] DateOnly StartedOn, [property: JsonRequired] decimal OccupiedAreaM2, Guid? ResponsibleParticipantId);
internal sealed class OpenCultivationCycleCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<OpenCultivationCycleCommandHandler> logger)
{
    public async Task<Result<CreatedCultivationCycleResponse>> HandleAsync(Guid gardenId, OpenCultivationCycleRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.OpenCultivationCycle", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId });
        Result<CreatedCultivationCycleResponse> Reject(Error error) => process.Complete(Result<CreatedCultivationCycleResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = CultivationCycleValues.Create(request.BedId, request.SpeciesId, request.CultivarId, request.StartedOn, request.OccupiedAreaM2, null, request.ResponsibleParticipantId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        if (request.OperationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var payload = AuditSnapshots.Capture(values);
        var existing = await db.CultivationCycles.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == request.OperationId, cancellationToken);
        if (existing is not null)
        {
            using var original = System.Text.Json.JsonDocument.Parse(existing.CreationPayload);
            using var candidatePayload = System.Text.Json.JsonDocument.Parse(payload);
            if (!System.Text.Json.JsonElement.DeepEquals(original.RootElement, candidatePayload.RootElement)) return Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<CreatedCultivationCycleResponse>.Success(new(false, CultivationCycleProjection.Map(existing))));
        }
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CultivationRecordContext.ResolveCultivationCycleAsync(db, gardenId, values, Guid.Empty, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var candidate = CultivationCycle.Create(gardenId, request.OperationId, values, await author.ResolveAsync(cancellationToken), now, payload, context.Value.BedConfigurationId, context.Value.ReferenceAreaM2);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        db.CultivationCycles.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "CultivationCycle", entity.Id, entity.RecordedByUserId, now, null, AuditSnapshots.Capture(CultivationCycleProjection.Map(entity))));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("OpenCultivationCycleConfirmed", "Created", "Operação OpenCultivationCycle confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CreatedCultivationCycleResponse>.Success(new(true, CultivationCycleProjection.Map(entity))));
    }
}
