using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListIrrigationSectorHistory;

internal sealed record IrrigationSectorHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, DateTimeOffset OccurredAt, Guid UserId, string? Before, string After);
internal sealed class ListIrrigationSectorHistoryQueryHandler(AppDbContext db, ILogger<ListIrrigationSectorHistoryQueryHandler> logger)
{
    public async Task<Result<Page<IrrigationSectorHistoryResponse>>> HandleAsync(Guid gardenId, Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListIrrigationSectorHistory", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<Page<IrrigationSectorHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<IrrigationSectorHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.IrrigationSectors.AsNoTracking().AnyAsync(x => x.Id == id && x.GardenId == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando histórico paginado do cadastro");
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "IrrigationSector" && x.EntityKey == id && x.GardenId == gardenId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new IrrigationSectorHistoryResponse(x.Id, x.Operation, x.Reason, x.OccurredAt, x.UserId, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<IrrigationSectorHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
