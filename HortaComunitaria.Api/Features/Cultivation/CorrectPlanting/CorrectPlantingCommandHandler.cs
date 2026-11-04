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
namespace HortaComunitaria.Api.Features.Cultivation.CorrectPlanting;

internal sealed record CorrectPlantingRequest([property: JsonRequired] Guid CycleId, [property: JsonRequired] Guid? PropagationLotId, [property: JsonRequired] Guid? ReplacedPlantingId, [property: JsonRequired] DateOnly PlantedOn, [property: JsonRequired] PlantingKind Kind, [property: JsonRequired] int PlantedQuantity, [property: JsonRequired] decimal? PlantSpacingCm, [property: JsonRequired] decimal? RowSpacingCm, [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectPlantingCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CorrectPlantingCommandHandler> logger)
{
    public async Task<Result<PlantingResponse>> HandleAsync(Guid gardenId, Guid id, CorrectPlantingRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CorrectPlanting", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<PlantingResponse> Reject(Error error) => process.Complete(Result<PlantingResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = PlantingValues.Create(request.CycleId, request.PropagationLotId, request.ReplacedPlantingId, request.PlantedOn, request.Kind, request.PlantedQuantity, request.PlantSpacingCm, request.RowSpacingCm, request.ResponsibleParticipantId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.Plantings.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(PlantingProjection.Map(entity));
        var dependencies = await CultivationDependencies.CheckPlantingAsync(db, gardenId, id, values, cancellationToken);
        if (!dependencies.IsSuccess) return Reject(dependencies.Error);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CultivationRecordContext.ResolvePlantingAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<PlantingResponse>.Success(PlantingProjection.Map(entity)));
        }
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Planting", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(PlantingProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectPlantingConfirmed", "Updated", "Operação CorrectPlanting confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<PlantingResponse>.Success(PlantingProjection.Map(entity)));
    }
}
