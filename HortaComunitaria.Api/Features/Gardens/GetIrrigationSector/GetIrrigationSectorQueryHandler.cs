using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.GetIrrigationSector;

internal sealed class GetIrrigationSectorQueryHandler(AppDbContext db, ILogger<GetIrrigationSectorQueryHandler> logger)
{
    public async Task<Result<IrrigationSectorResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.GetIrrigationSector", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<IrrigationSectorResponse> Reject(Error error) => process.Complete(Result<IrrigationSectorResponse>.Failure(error));
        if (id == Guid.Empty || gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe do cadastro preservado");
        var entity = await db.IrrigationSectors.AsNoTracking().Where(x => x.Id == id && x.GardenId == gardenId).Select(IrrigationSectorProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return process.Complete(entity is null ? Result<IrrigationSectorResponse>.Failure(Error.NotFound()) : Result<IrrigationSectorResponse>.Success(entity));
    }
}
