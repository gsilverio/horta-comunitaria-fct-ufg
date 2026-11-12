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
namespace HortaComunitaria.Api.Features.Cultivation.InactivateSpecies;

internal sealed record InactivateSpeciesRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InactivateSpeciesCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InactivateSpeciesCommandHandler> logger)
{
    public async Task<Result<SpeciesResponse>> HandleAsync(Guid id, InactivateSpeciesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.InactivateSpecies", new Dictionary<string, object?> { });
        Result<SpeciesResponse> Reject(Error error) => process.Complete(Result<SpeciesResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.Species.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());

        var before = AuditSnapshots.Capture(SpeciesProjection.Map(entity));
        var applied = entity.Inactivate(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()));
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = true;
        var reasonValue = reason.Value.Value;
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "Species", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(SpeciesProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("InactivateSpeciesConfirmed", "Invalidated", "Operação InactivateSpecies confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<SpeciesResponse>.Success(SpeciesProjection.Map(entity)));
    }
}
