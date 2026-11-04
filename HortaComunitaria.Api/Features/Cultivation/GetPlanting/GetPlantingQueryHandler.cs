using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetPlanting;

internal sealed class GetPlantingQueryHandler(AppDbContext db, ILogger<GetPlantingQueryHandler> logger)
{
    public async Task<Result<PlantingResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetPlanting", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<PlantingResponse> Reject(Error error) => process.Complete(Result<PlantingResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.Plantings.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(PlantingProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<PlantingResponse>.Success(entity));
    }
}
