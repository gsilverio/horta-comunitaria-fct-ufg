using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetAgronomicObservation;

internal sealed class GetAgronomicObservationQueryHandler(AppDbContext db, ILogger<GetAgronomicObservationQueryHandler> logger)
{
    public async Task<Result<AgronomicObservationResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetAgronomicObservation", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<AgronomicObservationResponse> Reject(Error error) => process.Complete(Result<AgronomicObservationResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.AgronomicObservations.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(AgronomicObservationProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<AgronomicObservationResponse>.Success(entity));
    }
}
