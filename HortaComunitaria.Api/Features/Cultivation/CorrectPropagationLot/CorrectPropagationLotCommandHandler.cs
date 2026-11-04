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
namespace HortaComunitaria.Api.Features.Cultivation.CorrectPropagationLot;

internal sealed record CorrectPropagationLotRequest(string? Code, [property: JsonRequired] Guid SpeciesId, [property: JsonRequired] Guid? CultivarId, [property: JsonRequired] MaterialType MaterialType, [property: JsonRequired] string? Origin, [property: JsonRequired] DateOnly? ReceivedOn, [property: JsonRequired] string? Notes, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectPropagationLotCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CorrectPropagationLotCommandHandler> logger)
{
    public async Task<Result<PropagationLotResponse>> HandleAsync(Guid gardenId, Guid id, CorrectPropagationLotRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CorrectPropagationLot", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<PropagationLotResponse> Reject(Error error) => process.Complete(Result<PropagationLotResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.PropagationLots.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        if (!await db.Species.AnyAsync(x => x.Id == request.SpeciesId && x.IsActive, cancellationToken) ||
            request.CultivarId is not null && !await db.Cultivars.AnyAsync(x => x.Id == request.CultivarId && x.SpeciesId == request.SpeciesId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if ((entity.SpeciesId != request.SpeciesId || entity.CultivarId != request.CultivarId) && await db.Plantings.AnyAsync(x => x.PropagationLotId == id && x.Validity == RecordValidity.Valid, cancellationToken)) return Reject(RecordRules.HasDependents("Origem com plantios válidos exige revisão dos dependentes antes de trocar espécie/cultivar."));
        var before = AuditSnapshots.Capture(PropagationLotProjection.Map(entity));
        var applied = entity.Update(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()), request.Code ?? entity.Code, request.SpeciesId, request.CultivarId, request.MaterialType, request.Origin, request.ReceivedOn, request.Notes);
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = applied.Value;
        var reasonValue = reason.Value.Value;
        if (changed)
        {
            if (await db.PropagationLots.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code && x.Id != entity.Id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "PropagationLot", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(PropagationLotProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectPropagationLotConfirmed", "Updated", "Operação CorrectPropagationLot confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<PropagationLotResponse>.Success(PropagationLotProjection.Map(entity)));
    }
}
