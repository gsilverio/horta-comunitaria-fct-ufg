using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
namespace HortaComunitaria.Api.Features.Costs.RegisterFinancialEntry;

internal sealed record RegisterFinancialEntryRequest([property: JsonRequired] Guid OperationId, [property: JsonRequired] DateOnly EntryDate, [property: JsonRequired] string Description, [property: JsonRequired] string Category, [property: JsonRequired] decimal AmountBrl, [property: JsonRequired] FinancialNature Nature, Guid? BedId, Guid? CycleId);
internal sealed class RegisterFinancialEntryCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<RegisterFinancialEntryCommandHandler> logger)
{
    public async Task<Result<CreatedFinancialEntryResponse>> HandleAsync(Guid gardenId, RegisterFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Costs.RegisterFinancialEntry", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId });
        Result<CreatedFinancialEntryResponse> Reject(Error error) => process.Complete(Result<CreatedFinancialEntryResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = FinancialEntryValues.Create(request.EntryDate, request.Description, request.Category, request.AmountBrl, request.Nature, request.BedId, request.CycleId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        if (request.OperationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var payload = AuditSnapshots.Capture(values);
        var existing = await db.FinancialEntries.AsNoTracking().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == request.OperationId, cancellationToken);
        if (existing is not null)
        {
            using var original = System.Text.Json.JsonDocument.Parse(existing.CreationPayload);
            using var candidatePayload = System.Text.Json.JsonDocument.Parse(payload);
            if (!System.Text.Json.JsonElement.DeepEquals(original.RootElement, candidatePayload.RootElement)) return Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<CreatedFinancialEntryResponse>.Success(new(false, FinancialEntryProjection.Map(existing))));
        }
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CostsRecordContext.ResolveFinancialEntryAsync(db, gardenId, values, Guid.Empty, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var candidate = FinancialEntry.Create(gardenId, request.OperationId, values, await author.ResolveAsync(cancellationToken), now, payload);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        db.FinancialEntries.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "FinancialEntry", entity.Id, entity.RecordedByUserId, now, null, AuditSnapshots.Capture(FinancialEntryProjection.Map(entity))));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("RegisterFinancialEntryConfirmed", "Created", "Operação RegisterFinancialEntry confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CreatedFinancialEntryResponse>.Success(new(true, FinancialEntryProjection.Map(entity))));
    }
}
