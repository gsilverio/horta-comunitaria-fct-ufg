using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.ListHarvestHistory;

internal sealed record HarvestHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListHarvestHistoryQueryHandler(AppDbContext db, ILogger<ListHarvestHistoryQueryHandler> logger)
{
    public async Task<Result<Page<HarvestHistoryResponse>>> HandleAsync(Guid gardenId, Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListHarvestHistory", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<Page<HarvestHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<HarvestHistoryResponse>>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Harvests.AsNoTracking().AnyAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando histórico de inclusão e alterações do fato");
        var query = db.AuditEntries.AsNoTracking().Where(x => x.GardenId == gardenId && x.EntityType == "Harvest" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new HarvestHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<HarvestHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
