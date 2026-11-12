using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.CompleteDryingLot;

internal sealed record CompleteDryingLotRequest([property: JsonRequired] DateOnly EndedOn, [property: JsonRequired] decimal TotalDryMassKg, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CompleteDryingLotCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CompleteDryingLotCommandHandler> logger)
{
    public async Task<Result<DryingLotResponse>> HandleAsync(Guid gardenId, Guid id, CompleteDryingLotRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CompleteDryingLot", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<DryingLotResponse> Reject(Error error) => process.Complete(Result<DryingLotResponse>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("LoadingRecord", "Conferindo estado, versão e dependentes do registro");
        var entity = await db.DryingLots.Include(x => x.Items).AsSplitQuery().SingleOrDefaultAsync(x => x.Id == id && x.GardenId == gardenId, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());

        if (entity.Status == DryingStatus.Completed) return Reject(new("drying.already_completed", "Lote já concluído; use correção para revisar a pesagem.", ErrorCategory.Conflict));
        var candidate = DryingLotForm.Create(entity.Code, entity.Method, entity.StartedOn, request.EndedOn, request.TotalDryMassKg, entity.ResponsibleParticipantId, entity.Items.Select(x => new DryingItemRequest(x.HarvestId, x.AllocatedMassKg)).ToArray());
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var values = candidate.Value;
        await WriteLocks.HarvestsAsync(db, gardenId, entity.Items.Select(x => x.HarvestId), cancellationToken);
        var context = await CultivationRecordContext.ResolveDryingLotAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var before = AuditSnapshots.Capture(DryingLotProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "DryingLot", id, await author.ResolveAsync(cancellationToken), now,
                before, AuditSnapshots.Capture(DryingLotProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando novo estado e histórico da alteração");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CompleteDryingLotConfirmed", "Updated", "Operação CompleteDryingLot confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<DryingLotResponse>.Success(DryingLotProjection.Map(entity)));
    }
}
