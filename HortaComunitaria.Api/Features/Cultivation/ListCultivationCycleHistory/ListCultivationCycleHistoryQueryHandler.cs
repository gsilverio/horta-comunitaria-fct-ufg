using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.ListCultivationCycleHistory;

internal sealed record CultivationCycleHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListCultivationCycleHistoryQueryHandler(AppDbContext db, ILogger<ListCultivationCycleHistoryQueryHandler> logger)
{
    public async Task<Result<Page<CultivationCycleHistoryResponse>>> HandleAsync(Guid gardenId, Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListCultivationCycleHistory", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<Page<CultivationCycleHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<CultivationCycleHistoryResponse>>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.CultivationCycles.AsNoTracking().AnyAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando histórico de inclusão e alterações do fato");
        var query = db.AuditEntries.AsNoTracking().Where(x => x.GardenId == gardenId && x.EntityType == "CultivationCycle" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new CultivationCycleHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<CultivationCycleHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
