using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListIrrigationSectorCatalog;

internal sealed class ListIrrigationSectorCatalogQueryHandler(AppDbContext db, ILogger<ListIrrigationSectorCatalogQueryHandler> logger)
{
    public async Task<Result<Page<IrrigationSectorResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListIrrigationSectorCatalog", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<IrrigationSectorResponse>>.Failure(validation.Error));
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken))
            return process.Complete(Result<Page<IrrigationSectorResponse>>.Failure(Error.NotFound()));
        process.Step("Querying", "Consultando catálogo administrativo de setores");
        var query = db.IrrigationSectors.AsNoTracking().Where(x => x.GardenId == gardenId && (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(IrrigationSectorProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<IrrigationSectorResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}

