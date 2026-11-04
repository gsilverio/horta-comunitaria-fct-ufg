using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Environment.ListSamplingPointHistory;

internal sealed record SamplingPointHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListSamplingPointHistoryQueryHandler(AppDbContext db, ILogger<ListSamplingPointHistoryQueryHandler> logger)
{
    public async Task<Result<Page<SamplingPointHistoryResponse>>> HandleAsync(Guid gardenId, Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.ListSamplingPointHistory", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<Page<SamplingPointHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<SamplingPointHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        if (!await db.SamplingPoints.AsNoTracking().AnyAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "SamplingPoint" && x.EntityKey == id && x.GardenId == gardenId);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new SamplingPointHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<SamplingPointHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
