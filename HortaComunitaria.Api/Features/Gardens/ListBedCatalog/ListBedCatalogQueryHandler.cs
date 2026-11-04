using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListBedCatalog;

internal sealed class ListBedCatalogQueryHandler(AppDbContext db, ILogger<ListBedCatalogQueryHandler> logger)
{
    public async Task<Result<Page<BedResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListBedCatalog", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return process.Complete(Result<Page<BedResponse>>.Failure(valid.Error));
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return process.Complete(Result<Page<BedResponse>>.Failure(Error.NotFound()));
        process.Step("Querying", "Consultando catálogo administrativo de canteiros");
        var query = db.Beds.AsNoTracking().Where(x => x.GardenId == gardenId && (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(BedProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<BedResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
