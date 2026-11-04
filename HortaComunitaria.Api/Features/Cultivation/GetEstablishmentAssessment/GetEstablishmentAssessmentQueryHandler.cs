using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetEstablishmentAssessment;

internal sealed class GetEstablishmentAssessmentQueryHandler(AppDbContext db, ILogger<GetEstablishmentAssessmentQueryHandler> logger)
{
    public async Task<Result<EstablishmentAssessmentResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetEstablishmentAssessment", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<EstablishmentAssessmentResponse> Reject(Error error) => process.Complete(Result<EstablishmentAssessmentResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.EstablishmentAssessments.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(EstablishmentAssessmentProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<EstablishmentAssessmentResponse>.Success(entity));
    }
}
