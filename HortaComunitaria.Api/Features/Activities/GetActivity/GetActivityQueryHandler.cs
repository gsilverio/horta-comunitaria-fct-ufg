using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.GetActivity;

internal sealed class GetActivityQueryHandler(AppDbContext db, ILogger<GetActivityQueryHandler> logger)
{
    public async Task<Result<ActivityResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.GetActivity", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<ActivityResponse> Reject(Error error) => process.Complete(Result<ActivityResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.Activities.AsNoTracking().Include(x => x.Participants).Include(x => x.InputUsages).Include(x => x.Institutions).AsSplitQuery().Where(x => x.GardenId == gardenId && x.Id == id).Select(ActivityProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<ActivityResponse>.Success(entity));
    }
}
