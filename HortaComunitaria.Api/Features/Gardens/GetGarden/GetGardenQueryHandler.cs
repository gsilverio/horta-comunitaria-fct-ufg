using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.GetGarden;

internal sealed class GetGardenQueryHandler(AppDbContext db, ILogger<GetGardenQueryHandler> logger)
{
    public async Task<Result<GardenResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.GetGarden", new Dictionary<string, object?> { ["ResourceId"] = id });
        Result<GardenResponse> Reject(Error error) => process.Complete(Result<GardenResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe do cadastro preservado");
        var entity = await db.Gardens.AsNoTracking().Where(x => x.Id == id).Select(GardenProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return process.Complete(entity is null ? Result<GardenResponse>.Failure(Error.NotFound()) : Result<GardenResponse>.Success(entity));
    }
}
