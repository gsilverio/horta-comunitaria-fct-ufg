using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.GetEnvironmentalVariable;

internal sealed class GetEnvironmentalVariableQueryHandler(AppDbContext db, ILogger<GetEnvironmentalVariableQueryHandler> logger)
{
    public async Task<Result<EnvironmentalVariableResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.GetEnvironmentalVariable", new Dictionary<string, object?> { });
        Result<EnvironmentalVariableResponse> Reject(Error error) => process.Complete(Result<EnvironmentalVariableResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.EnvironmentalVariables.AsNoTracking().Where(x => x.Id == id).Select(EnvironmentalVariableProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<EnvironmentalVariableResponse>.Success(entity));
    }
}
