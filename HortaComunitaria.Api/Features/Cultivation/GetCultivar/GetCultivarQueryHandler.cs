using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetCultivar;

internal sealed class GetCultivarQueryHandler(AppDbContext db, ILogger<GetCultivarQueryHandler> logger)
{
    public async Task<Result<CultivarResponse>> HandleAsync(Guid speciesId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetCultivar", new Dictionary<string, object?> { ["SpeciesId"] = speciesId });
        Result<CultivarResponse> Reject(Error error) => process.Complete(Result<CultivarResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        if (!await db.Species.AsNoTracking().AnyAsync(x => x.Id == speciesId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.Cultivars.AsNoTracking().Where(x => x.SpeciesId == speciesId && x.Id == id).Select(CultivarProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<CultivarResponse>.Success(entity));
    }
}
