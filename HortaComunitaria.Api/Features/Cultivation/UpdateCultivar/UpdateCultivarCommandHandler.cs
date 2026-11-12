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
namespace HortaComunitaria.Api.Features.Cultivation.UpdateCultivar;

internal sealed record UpdateCultivarRequest([property: JsonRequired] string Name, [property: JsonRequired] string? Description, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class UpdateCultivarCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<UpdateCultivarCommandHandler> logger)
{
    public async Task<Result<CultivarResponse>> HandleAsync(Guid speciesId, Guid id, UpdateCultivarRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.UpdateCultivar", new Dictionary<string, object?> { ["SpeciesId"] = speciesId });
        Result<CultivarResponse> Reject(Error error) => process.Complete(Result<CultivarResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");
        if (!await db.Species.AnyAsync(x => x.Id == speciesId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.Cultivars.SingleOrDefaultAsync(x => x.SpeciesId == speciesId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());

        var before = AuditSnapshots.Capture(CultivarProjection.Map(entity));
        var applied = entity.Update(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()), request.Name, request.Description);
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = applied.Value;
        var reasonValue = reason.Value.Value;
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "Cultivar", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(CultivarProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("UpdateCultivarConfirmed", "Updated", "Operação UpdateCultivar confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CultivarResponse>.Success(CultivarProjection.Map(entity)));
    }
}
