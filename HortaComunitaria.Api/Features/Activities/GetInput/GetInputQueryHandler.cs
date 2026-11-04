using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.GetInput;

internal sealed class GetInputQueryHandler(AppDbContext db, ILogger<GetInputQueryHandler> logger)
{
    public async Task<Result<AgriculturalInputResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.GetInput", new Dictionary<string, object?> { });
        Result<AgriculturalInputResponse> Reject(Error error) => process.Complete(Result<AgriculturalInputResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.Inputs.AsNoTracking().Where(x => x.Id == id).Select(AgriculturalInputProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<AgriculturalInputResponse>.Success(entity));
    }
}
