using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.ListPropagationLots;

internal sealed class ListPropagationLotsQueryHandler(AppDbContext db, ILogger<ListPropagationLotsQueryHandler> logger)
{
    public async Task<Result<Page<PropagationLotResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListPropagationLots", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<PropagationLotResponse>> Reject(Error error) => process.Complete(Result<Page<PropagationLotResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var query = db.PropagationLots.AsNoTracking().Where(x => x.GardenId == gardenId && (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(PropagationLotProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<PropagationLotResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
