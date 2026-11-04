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
namespace HortaComunitaria.Api.Features.Cultivation.RegisterPropagationLot;

internal sealed record RegisterPropagationLotRequest(string? Code, [property: JsonRequired] Guid SpeciesId, Guid? CultivarId, [property: JsonRequired] MaterialType MaterialType, string? Origin, DateOnly? ReceivedOn, string? Notes);
internal sealed class RegisterPropagationLotCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<RegisterPropagationLotCommandHandler> logger)
{
    public async Task<Result<PropagationLotResponse>> HandleAsync(Guid gardenId, RegisterPropagationLotRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.RegisterPropagationLot", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<PropagationLotResponse> Reject(Error error) => process.Complete(Result<PropagationLotResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if (!await db.Species.AnyAsync(x => x.Id == request.SpeciesId && x.IsActive, cancellationToken) ||
                    request.CultivarId is not null && !await db.Cultivars.AnyAsync(x => x.Id == request.CultivarId && x.SpeciesId == request.SpeciesId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var candidate = PropagationLot.Create(gardenId, request.Code, request.SpeciesId, request.CultivarId, request.MaterialType, request.Origin, request.ReceivedOn, request.Notes);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.PropagationLots.Add(entity);
        if (changed)
        {
            if (await db.PropagationLots.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code && x.Id != entity.Id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "PropagationLot", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(PropagationLotProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("RegisterPropagationLotConfirmed", "Created", "Operação RegisterPropagationLot confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<PropagationLotResponse>.Success(PropagationLotProjection.Map(entity)));
    }
}
