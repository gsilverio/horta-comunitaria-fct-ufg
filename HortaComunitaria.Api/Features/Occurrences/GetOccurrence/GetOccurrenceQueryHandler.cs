using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Occurrences.Shared;
namespace HortaComunitaria.Api.Features.Occurrences.GetOccurrence;

internal sealed class GetOccurrenceQueryHandler(AppDbContext db, ILogger<GetOccurrenceQueryHandler> logger)
{
    public async Task<Result<OccurrenceResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Occurrences.GetOccurrence", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<OccurrenceResponse> Reject(Error error) => process.Complete(Result<OccurrenceResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.Occurrences.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(OccurrenceProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<OccurrenceResponse>.Success(entity));
    }
}
