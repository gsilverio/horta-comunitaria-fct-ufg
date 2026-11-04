using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListBedConfigurations;

internal sealed class ListBedConfigurationsQueryHandler(AppDbContext db, ILogger<ListBedConfigurationsQueryHandler> logger)
{
    public async Task<Result<Page<BedConfigurationResponse>>> HandleAsync(Guid gardenId, Guid bedId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListBedConfigurations", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["BedId"] = bedId });
        Result<Page<BedConfigurationResponse>> Reject(Error error) => process.Complete(Result<Page<BedConfigurationResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Beds.AsNoTracking().AnyAsync(x => x.Id == bedId && x.GardenId == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando vigências históricas do canteiro");
        var query = db.BedConfigurations.AsNoTracking().Where(x => x.GardenId == gardenId && x.BedId == bedId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ValidFrom).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(BedConfigurationProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<BedConfigurationResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
