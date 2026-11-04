using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.GetEnvironmentalMeasurement;

internal sealed class GetEnvironmentalMeasurementQueryHandler(AppDbContext db, ILogger<GetEnvironmentalMeasurementQueryHandler> logger)
{
    public async Task<Result<EnvironmentalMeasurementResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.GetEnvironmentalMeasurement", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<EnvironmentalMeasurementResponse> Reject(Error error) => process.Complete(Result<EnvironmentalMeasurementResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.EnvironmentalMeasurements.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(EnvironmentalMeasurementProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<EnvironmentalMeasurementResponse>.Success(entity));
    }
}
