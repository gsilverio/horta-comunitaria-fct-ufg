using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;

namespace HortaComunitaria.Api.Features.Irrigation.GetIrrigation;

internal sealed class GetIrrigationQueryHandler(AppDbContext db, ILogger<GetIrrigationQueryHandler> logger)
{
    public async Task<Result<IrrigationDetailResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Irrigation.GetIrrigation",
            new Dictionary<string, object?> { ["GardenId"] = gardenId, ["IrrigationId"] = id });
        if (gardenId == Guid.Empty || id == Guid.Empty)
            return process.Complete(Result<IrrigationDetailResponse>.Failure(Error.Validation("id", "Identificador obrigatório.")));
        process.Step("Querying", "Consultando detalhe da irrigação {IrrigationId}", id);
        var detail = await IrrigationDetailProjection.LoadAsync(db, gardenId, id, cancellationToken);
        return process.Complete(detail is null ? Result<IrrigationDetailResponse>.Failure(Error.NotFound()) :
            Result<IrrigationDetailResponse>.Success(detail));
    }
}
