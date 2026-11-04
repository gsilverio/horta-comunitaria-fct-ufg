using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Costs.Shared;
namespace HortaComunitaria.Api.Features.Costs.GetFinancialEntry;

internal sealed class GetFinancialEntryQueryHandler(AppDbContext db, ILogger<GetFinancialEntryQueryHandler> logger)
{
    public async Task<Result<FinancialEntryResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Costs.GetFinancialEntry", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<FinancialEntryResponse> Reject(Error error) => process.Complete(Result<FinancialEntryResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        var entity = await db.FinancialEntries.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(FinancialEntryProjection.Select).SingleOrDefaultAsync(cancellationToken);
        return entity is null ? Reject(Error.NotFound()) : process.Complete(Result<FinancialEntryResponse>.Success(entity));
    }
}
