using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListGardenCatalog;

internal sealed class ListGardenCatalogQueryHandler(AppDbContext db, ILogger<ListGardenCatalogQueryHandler> logger)
{
    public async Task<Result<Page<GardenResponse>>> HandleAsync(PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListGardenCatalog");
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<GardenResponse>>.Failure(validation.Error));
        process.Step("Querying", "Consultando catálogo administrativo de hortas");
        var query = db.Gardens.AsNoTracking().Where(x => (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(GardenProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<GardenResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}

