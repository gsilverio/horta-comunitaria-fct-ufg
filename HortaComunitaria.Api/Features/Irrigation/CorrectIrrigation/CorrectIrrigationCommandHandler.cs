using System.Data;
using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Irrigation.CorrectIrrigation;

/// <summary>Substituição completa do formulário; campos não coletados devem vir explicitamente como null.</summary>
internal sealed record CorrectIrrigationRequest(
    [property: JsonRequired] Guid SectorId, [property: JsonRequired] Guid[] BedIds,
    [property: JsonRequired] DateTimeOffset StartedAt, [property: JsonRequired] DateTimeOffset? EndedAt,
    [property: JsonRequired] decimal? ActiveDurationMinutes, [property: JsonRequired] decimal? VolumeLiters,
    [property: JsonRequired] decimal? FlowRateLitersPerMinute, [property: JsonRequired] VolumeSource? VolumeSource,
    [property: JsonRequired] string? VolumeMethod, [property: JsonRequired] IrrigationStatus Status,
    [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] string? Notes,
    [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);

internal sealed class CorrectIrrigationCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock,
    ILogger<CorrectIrrigationCommandHandler> logger)
{
    public async Task<Result<IrrigationDetailResponse>> HandleAsync(Guid gardenId, Guid id, CorrectIrrigationRequest request,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Irrigation.CorrectIrrigation", new Dictionary<string, object?>
        { ["GardenId"] = gardenId, ["IrrigationId"] = id, ["ExpectedVersion"] = request.ExpectedVersion, ["SectorId"] = request.SectorId });
        Result<IrrigationDetailResponse> Reject(Error error) => process.Complete(Result<IrrigationDetailResponse>.Failure(error));
        process.Step("ValidatingRequest", "Validando a correção e a versão esperada {ExpectedVersion}", request.ExpectedVersion);
        if (id == Guid.Empty || request.ExpectedVersion < 1)
            return Reject(Error.Validation("expectedVersion", "Informe id válido e versão esperada positiva."));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var identifiers = IrrigationContextResolver.ValidateIds(gardenId, request.SectorId, request.BedIds, request.ResponsibleParticipantId);
        if (!identifiers.IsSuccess) return Reject(identifiers.Error);
        var values = IrrigationValues.Create(InstantPrecision.ForPersistence(request.StartedAt), InstantPrecision.ForPersistence(request.EndedAt), request.ActiveDurationMinutes, request.VolumeLiters,
            request.FlowRateLitersPerMinute, request.VolumeSource, request.VolumeMethod, request.Status, request.Notes);
        if (!values.IsSuccess) return Reject(values.Error);
        process.Step("LoadingRecord", "Carregando irrigação {IrrigationId} e abrindo transação de correção", id);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken, shared: true);
        await WriteLocks.BedsAsync(db, gardenId, request.BedIds, cancellationToken, shared: true);
        var irrigation = await db.Irrigations.Include(x => x.Beds).AsSingleQuery()
            .SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (irrigation is null) return Reject(Error.NotFound());
        var eligible = irrigation.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        process.Step("ResolvingContext", "Conferindo setor e vínculos da proposta de correção");
        var assignments = await IrrigationContextResolver.ResolveAsync(db, gardenId, request.SectorId, request.BedIds,
            request.ResponsibleParticipantId, values.Value.StartedAt, cancellationToken);
        if (!assignments.IsSuccess) return Reject(assignments.Error);
        if (irrigation.IrrigationSectorId != request.SectorId && await db.Occurrences.AnyAsync(x => x.GardenId == gardenId && x.IrrigationId == id && x.Validity == RecordValidity.Valid, cancellationToken))
            return Reject(RecordRules.HasDependents("Há ocorrência válida cujo contexto deriva do setor desta irrigação."));
        var before = IrrigationSnapshots.Capture(irrigation);
        var previousBeds = irrigation.Beds.ToArray();
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = irrigation.Correct(request.ExpectedVersion, reason.Value, request.SectorId, values.Value,
            request.ResponsibleParticipantId, assignments.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            process.Step("Persisting", "Persistindo correção de irrigação {IrrigationId} e histórico", id);
            var selectedIds = irrigation.Beds.Select(x => x.BedId).ToHashSet();
            var previousIds = previousBeds.Select(x => x.BedId).ToHashSet();
            db.IrrigationBeds.RemoveRange(previousBeds.Where(x => !selectedIds.Contains(x.BedId)));
            db.IrrigationBeds.AddRange(irrigation.Beds.Where(x => !previousIds.Contains(x.BedId)));
            db.AuditEntries.Add(AuditEntry.CreateChange(irrigation, AuditOperation.Corrected, reason.Value,
                await author.ResolveAsync(cancellationToken), now, before, IrrigationSnapshots.Capture(irrigation)).Value);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Reject(new("irrigation.version_conflict", "O registro foi alterado. Recarregue antes de revisar.", ErrorCategory.Conflict));
            }
        }
        else
        {
            process.Step("CheckingVersion", "Conferindo sob lock a versão de uma correção sem mudanças");
            // Sem UPDATE artificial: confere a versão sob lock antes de responder ao no-op.
            var currentVersion = await db.Database.SqlQuery<int>($"""
                SELECT "Version" AS "Value" FROM irrigations
                WHERE "GardenId" = {gardenId} AND "Id" = {id} FOR UPDATE
                """).TagWith("CorrectIrrigation.NoChange").SingleOrDefaultAsync(cancellationToken);
            if (currentVersion != request.ExpectedVersion)
                return Reject(new("irrigation.version_conflict", "O registro foi alterado. Recarregue antes de revisar.", ErrorCategory.Conflict));
        }
        process.Step("ConfirmingTransaction", "Consultando o resultado e confirmando a transação de correção");
        var detail = await IrrigationDetailProjection.LoadAsync(db, gardenId, id, cancellationToken)
            ?? throw new InvalidOperationException("Irrigação corrigida não foi localizada.");
        await transaction.CommitAsync(cancellationToken);
        process.Event(changed.Value ? "IrrigationCorrected" : "IrrigationCorrectionUnchanged", changed.Value ? "Corrected" : "Unchanged",
            changed.Value ? "Irrigação {IrrigationId} corrigida; versão confirmada {Version}" : "Irrigação {IrrigationId} sem alterações; versão preservada {Version}", id, detail.Version);
        return process.Complete(Result<IrrigationDetailResponse>.Success(detail));
    }
}
