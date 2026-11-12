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
namespace HortaComunitaria.Api.Features.Cultivation.CreateSpecies;

internal sealed record CreateSpeciesRequest([property: JsonRequired] string CommonName, string? ScientificName, string? Description);
internal sealed class CreateSpeciesCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CreateSpeciesCommandHandler> logger)
{
    public async Task<Result<SpeciesResponse>> HandleAsync(CreateSpeciesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CreateSpecies", new Dictionary<string, object?> { });
        Result<SpeciesResponse> Reject(Error error) => process.Complete(Result<SpeciesResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var candidate = Species.Create(request.CommonName, request.ScientificName, request.Description);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.Species.Add(entity);
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "Species", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(SpeciesProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateSpeciesConfirmed", "Created", "Operação CreateSpecies confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<SpeciesResponse>.Success(SpeciesProjection.Map(entity)));
    }
}
