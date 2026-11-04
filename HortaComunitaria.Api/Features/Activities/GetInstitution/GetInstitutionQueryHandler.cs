using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.GetInstitution;

internal sealed class GetInstitutionQueryHandler(AppDbContext db, ILogger<GetInstitutionQueryHandler> logger)
{
    public async Task<Result<InstitutionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.GetInstitution", new Dictionary<string, object?> { });
        Result<InstitutionResponse> Reject(Error error) => process.Complete(Result<InstitutionResponse>.Failure(error));
        if (id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var entity = await db.Institutions.AsNoTracking().Where(x => x.Id == id).Select(InstitutionProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<InstitutionResponse>.Success(entity));
    }
}
