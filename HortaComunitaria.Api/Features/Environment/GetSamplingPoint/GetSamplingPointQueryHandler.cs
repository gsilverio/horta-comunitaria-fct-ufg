using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.GetSamplingPoint;

internal sealed class GetSamplingPointQueryHandler(AppDbContext db, ILogger<GetSamplingPointQueryHandler> logger)
{
    public async Task<Result<SamplingPointResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.GetSamplingPoint", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<SamplingPointResponse> Reject(Error error) => process.Complete(Result<SamplingPointResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.SamplingPoints.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(SamplingPointProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<SamplingPointResponse>.Success(entity));
    }
}
