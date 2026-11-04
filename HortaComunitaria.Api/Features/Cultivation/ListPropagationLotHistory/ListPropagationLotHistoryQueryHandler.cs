using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.ListPropagationLotHistory;

internal sealed record PropagationLotHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListPropagationLotHistoryQueryHandler(AppDbContext db, ILogger<ListPropagationLotHistoryQueryHandler> logger)
{
    public async Task<Result<Page<PropagationLotHistoryResponse>>> HandleAsync(Guid gardenId, Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListPropagationLotHistory", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<Page<PropagationLotHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<PropagationLotHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        if (!await db.PropagationLots.AsNoTracking().AnyAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "PropagationLot" && x.EntityKey == id && x.GardenId == gardenId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new PropagationLotHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<PropagationLotHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
