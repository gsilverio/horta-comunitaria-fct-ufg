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
namespace HortaComunitaria.Api.Features.Cultivation.CreateCultivar;

internal sealed record CreateCultivarRequest([property: JsonRequired] string Name, string? Description);
internal sealed class CreateCultivarCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CreateCultivarCommandHandler> logger)
{
    public async Task<Result<CultivarResponse>> HandleAsync(Guid speciesId, CreateCultivarRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CreateCultivar", new Dictionary<string, object?> { ["SpeciesId"] = speciesId });
        Result<CultivarResponse> Reject(Error error) => process.Complete(Result<CultivarResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");
        if (!await db.Species.AnyAsync(x => x.Id == speciesId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());

        var candidate = Cultivar.Create(speciesId, request.Name, request.Description);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.Cultivars.Add(entity);
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "Cultivar", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(CultivarProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateCultivarConfirmed", "Created", "Operação CreateCultivar confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CultivarResponse>.Success(CultivarProjection.Map(entity)));
    }
}
