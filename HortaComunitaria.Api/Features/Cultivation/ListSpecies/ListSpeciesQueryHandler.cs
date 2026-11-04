using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.ListSpecies;

internal sealed class ListSpeciesQueryHandler(AppDbContext db, ILogger<ListSpeciesQueryHandler> logger)
{
    public async Task<Result<Page<SpeciesResponse>>> HandleAsync(PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListSpecies", new Dictionary<string, object?> { });
        Result<Page<SpeciesResponse>> Reject(Error error) => process.Complete(Result<Page<SpeciesResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var query = db.Species.AsNoTracking().Where(x => (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.CommonName).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(SpeciesProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<SpeciesResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
