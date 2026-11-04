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
namespace HortaComunitaria.Api.Features.Cultivation.CloseCultivationCycle;

internal sealed record CloseCultivationCycleRequest([property: JsonRequired] DateOnly EndedOn, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CloseCultivationCycleCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CloseCultivationCycleCommandHandler> logger)
{
    public async Task<Result<CultivationCycleResponse>> HandleAsync(Guid gardenId, Guid id, CloseCultivationCycleRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CloseCultivationCycle", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<CultivationCycleResponse> Reject(Error error) => process.Complete(Result<CultivationCycleResponse>.Failure(error));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("LoadingRecord", "Conferindo estado, versão e dependentes do registro");
        var entity = await db.CultivationCycles.SingleOrDefaultAsync(x => x.Id == id && x.GardenId == gardenId, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());

        if (entity.Status == CultivationStatus.Closed) return Reject(new("cycle.already_closed", "Ciclo já encerrado; use correção com motivo para revisar a data.", ErrorCategory.Conflict));
        var candidate = CultivationCycleValues.Create(entity.BedId, entity.SpeciesId, entity.CultivarId, entity.StartedOn, entity.OccupiedAreaM2, request.EndedOn, entity.ResponsibleParticipantId);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var values = candidate.Value;
        var dependencies = await CultivationDependencies.CheckCultivationCycleAsync(db, gardenId, id, values, cancellationToken);
        if (!dependencies.IsSuccess) return Reject(dependencies.Error);
        var context = await CultivationRecordContext.ResolveCultivationCycleAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var before = AuditSnapshots.Capture(CultivationCycleProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now, context.Value.BedConfigurationId, context.Value.ReferenceAreaM2);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "CultivationCycle", id, await author.ResolveAsync(cancellationToken), now,
                before, AuditSnapshots.Capture(CultivationCycleProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando novo estado e histórico da alteração");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CloseCultivationCycleConfirmed", "Updated", "Operação CloseCultivationCycle confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CultivationCycleResponse>.Success(CultivationCycleProjection.Map(entity)));
    }
}
