using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListBedSectorAssignments;

internal sealed class ListBedSectorAssignmentsQueryHandler(AppDbContext db, ILogger<ListBedSectorAssignmentsQueryHandler> logger)
{
    public async Task<Result<Page<BedSectorAssignmentResponse>>> HandleAsync(Guid gardenId, Guid bedId, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListBedSectorAssignments", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["BedId"] = bedId });
        Result<Page<BedSectorAssignmentResponse>> Reject(Error error) => process.Complete(Result<Page<BedSectorAssignmentResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Beds.AsNoTracking().AnyAsync(x => x.Id == bedId && x.GardenId == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando vigências históricas do canteiro");
        var query = db.BedSectorAssignments.AsNoTracking().Where(x => x.GardenId == gardenId && x.BedId == bedId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ValidFrom).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(BedSectorAssignmentProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<BedSectorAssignmentResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
