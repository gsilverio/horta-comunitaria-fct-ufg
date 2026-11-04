using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListGardenHistory;

internal sealed record GardenHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, DateTimeOffset OccurredAt, Guid UserId, string? Before, string After);
internal sealed class ListGardenHistoryQueryHandler(AppDbContext db, ILogger<ListGardenHistoryQueryHandler> logger)
{
    public async Task<Result<Page<GardenHistoryResponse>>> HandleAsync(Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListGardenHistory", new Dictionary<string, object?> { ["ResourceId"] = id });
        Result<Page<GardenHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<GardenHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando histórico paginado do cadastro");
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "Garden" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new GardenHistoryResponse(x.Id, x.Operation, x.Reason, x.OccurredAt, x.UserId, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<GardenHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
