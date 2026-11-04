using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetCultivationCycle;

internal sealed class GetCultivationCycleQueryHandler(AppDbContext db, ILogger<GetCultivationCycleQueryHandler> logger)
{
    public async Task<Result<CultivationCycleResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetCultivationCycle", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<CultivationCycleResponse> Reject(Error error) => process.Complete(Result<CultivationCycleResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.CultivationCycles.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(CultivationCycleProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<CultivationCycleResponse>.Success(entity));
    }
}
