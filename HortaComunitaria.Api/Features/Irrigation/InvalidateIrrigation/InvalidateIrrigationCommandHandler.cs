using System.Data;
using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Irrigation.InvalidateIrrigation;

/// <summary>Invalidação exige a versão observada e motivo; não recebe autoria do cliente.</summary>
internal sealed record InvalidateIrrigationRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);

internal sealed class InvalidateIrrigationCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock,
    ILogger<InvalidateIrrigationCommandHandler> logger)
{
    public async Task<Result<IrrigationDetailResponse>> HandleAsync(Guid gardenId, Guid id, InvalidateIrrigationRequest request,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Irrigation.InvalidateIrrigation", new Dictionary<string, object?>
        { ["GardenId"] = gardenId, ["IrrigationId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<IrrigationDetailResponse> Reject(Error error) => process.Complete(Result<IrrigationDetailResponse>.Failure(error));
        process.Step("ValidatingRequest", "Validando invalidação e versão esperada {ExpectedVersion}", request.ExpectedVersion);
        if (gardenId == Guid.Empty || id == Guid.Empty || request.ExpectedVersion < 1)
            return Reject(Error.Validation("expectedVersion", "Informe identificadores válidos e versão esperada positiva."));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        process.Step("LoadingRecord", "Carregando irrigação {IrrigationId} para invalidação", id);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken, shared: true);
        var irrigation = await db.Irrigations.Include(x => x.Beds).AsSingleQuery()
            .SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (irrigation is null) return Reject(Error.NotFound());
        if (await db.Occurrences.AnyAsync(x => x.GardenId == gardenId && x.IrrigationId == id && x.Validity == RecordValidity.Valid, cancellationToken))
            return Reject(RecordRules.HasDependents("Há ocorrência válida vinculada à irrigação."));
        var before = IrrigationSnapshots.Capture(irrigation);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = irrigation.Invalidate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        process.Step("Persisting", "Persistindo invalidação de irrigação {IrrigationId} e histórico", id);
        db.AuditEntries.Add(AuditEntry.CreateChange(irrigation, AuditOperation.Invalidated, reason.Value,
            await author.ResolveAsync(cancellationToken), now, before, IrrigationSnapshots.Capture(irrigation)).Value);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Reject(new("irrigation.version_conflict", "O registro foi alterado. Recarregue antes de revisar.", ErrorCategory.Conflict));
        }
        process.Step("ConfirmingTransaction", "Consultando o resultado e confirmando a transação de invalidação");
        var detail = await IrrigationDetailProjection.LoadAsync(db, gardenId, id, cancellationToken)
            ?? throw new InvalidOperationException("Irrigação invalidada não foi localizada.");
        await transaction.CommitAsync(cancellationToken);
        process.Event("IrrigationInvalidated", "Invalidated", "Irrigação {IrrigationId} invalidada; histórico preservado na versão {Version}", id, detail.Version);
        return process.Complete(Result<IrrigationDetailResponse>.Success(detail));
    }
}
