using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
namespace HortaComunitaria.Api.Features.Costs.CorrectFinancialEntry;

internal sealed record CorrectFinancialEntryRequest([property: JsonRequired] DateOnly EntryDate, [property: JsonRequired] string Description, [property: JsonRequired] string Category, [property: JsonRequired] decimal AmountBrl, [property: JsonRequired] FinancialNature Nature, [property: JsonRequired] Guid? BedId, [property: JsonRequired] Guid? CycleId, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectFinancialEntryCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CorrectFinancialEntryCommandHandler> logger)
{
    public async Task<Result<FinancialEntryResponse>> HandleAsync(Guid gardenId, Guid id, CorrectFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Costs.CorrectFinancialEntry", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<FinancialEntryResponse> Reject(Error error) => process.Complete(Result<FinancialEntryResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = FinancialEntryValues.Create(request.EntryDate, request.Description, request.Category, request.AmountBrl, request.Nature, request.BedId, request.CycleId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.FinancialEntries.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(FinancialEntryProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CostsRecordContext.ResolveFinancialEntryAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<FinancialEntryResponse>.Success(FinancialEntryProjection.Map(entity)));
        }
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "FinancialEntry", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(FinancialEntryProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectFinancialEntryConfirmed", "Updated", "Operação CorrectFinancialEntry confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<FinancialEntryResponse>.Success(FinancialEntryProjection.Map(entity)));
    }
}
