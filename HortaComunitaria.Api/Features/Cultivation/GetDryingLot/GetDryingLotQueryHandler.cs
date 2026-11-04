using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetDryingLot;

internal sealed class GetDryingLotQueryHandler(AppDbContext db, ILogger<GetDryingLotQueryHandler> logger)
{
    public async Task<Result<DryingLotResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetDryingLot", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<DryingLotResponse> Reject(Error error) => process.Complete(Result<DryingLotResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.DryingLots.AsNoTracking().Include(x => x.Items).AsSplitQuery().Where(x => x.GardenId == gardenId && x.Id == id).Select(DryingLotProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<DryingLotResponse>.Success(entity));
    }
}
