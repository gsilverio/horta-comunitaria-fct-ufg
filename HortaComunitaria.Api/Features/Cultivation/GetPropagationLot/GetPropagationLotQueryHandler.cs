using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetPropagationLot;

internal sealed class GetPropagationLotQueryHandler(AppDbContext db, ILogger<GetPropagationLotQueryHandler> logger)
{
    public async Task<Result<PropagationLotResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetPropagationLot", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<PropagationLotResponse> Reject(Error error) => process.Complete(Result<PropagationLotResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.PropagationLots.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(PropagationLotProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<PropagationLotResponse>.Success(entity));
    }
}
