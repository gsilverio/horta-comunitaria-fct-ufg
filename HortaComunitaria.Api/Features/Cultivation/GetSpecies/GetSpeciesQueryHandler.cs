using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetSpecies;

internal sealed class GetSpeciesQueryHandler(AppDbContext db, ILogger<GetSpeciesQueryHandler> logger)
{
    public async Task<Result<SpeciesResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetSpecies", new Dictionary<string, object?> { });
        Result<SpeciesResponse> Reject(Error error) => process.Complete(Result<SpeciesResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.Species.AsNoTracking().Where(x => x.Id == id).Select(SpeciesProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<SpeciesResponse>.Success(entity));
    }
}
