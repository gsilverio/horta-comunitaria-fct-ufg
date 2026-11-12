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
namespace HortaComunitaria.Api.Features.Cultivation.CorrectDryingLot;

internal sealed record CorrectDryingLotRequest(string? Code, [property: JsonRequired] string? Method, [property: JsonRequired] DateOnly StartedOn, [property: JsonRequired] DateOnly? EndedOn, [property: JsonRequired] decimal? TotalDryMassKg, [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] DryingItemRequest[]? Items, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectDryingLotCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CorrectDryingLotCommandHandler> logger)
{
    public async Task<Result<DryingLotResponse>> HandleAsync(Guid gardenId, Guid id, CorrectDryingLotRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CorrectDryingLot", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<DryingLotResponse> Reject(Error error) => process.Complete(Result<DryingLotResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.DryingLots.Include(x => x.Items).AsSplitQuery().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var proposed = DryingLotForm.Create(request.Code ?? entity.Code, request.Method, request.StartedOn, request.EndedOn, request.TotalDryMassKg, request.ResponsibleParticipantId, request.Items);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var previousItems = entity.Items.ToArray();
        await WriteLocks.HarvestsAsync(db, gardenId, previousItems.Select(x => x.HarvestId).Concat(values.Items.Select(x => x.HarvestId)), cancellationToken);
        var before = AuditSnapshots.Capture(DryingLotProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CultivationRecordContext.ResolveDryingLotAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<DryingLotResponse>.Success(DryingLotProjection.Map(entity)));
        }
        db.DryingItems.RemoveRange(previousItems.Where(x => !entity.Items.Any(current => current.HarvestId == x.HarvestId)));
        db.DryingItems.AddRange(entity.Items.Where(x => !previousItems.Any(previous => previous.HarvestId == x.HarvestId)));
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "DryingLot", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(DryingLotProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectDryingLotConfirmed", "Updated", "Operação CorrectDryingLot confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<DryingLotResponse>.Success(DryingLotProjection.Map(entity)));
    }
}
