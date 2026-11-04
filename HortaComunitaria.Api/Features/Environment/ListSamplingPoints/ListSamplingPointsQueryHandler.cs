using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.ListSamplingPoints;

internal sealed class ListSamplingPointsQueryHandler(AppDbContext db, ILogger<ListSamplingPointsQueryHandler> logger)
{
    public async Task<Result<Page<SamplingPointResponse>>> HandleAsync(Guid gardenId, PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.ListSamplingPoints", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<SamplingPointResponse>> Reject(Error error) => process.Complete(Result<Page<SamplingPointResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var query = db.SamplingPoints.AsNoTracking().Where(x => x.GardenId == gardenId && (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(SamplingPointProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<SamplingPointResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
