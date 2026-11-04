using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.ListInputHistory;

internal sealed record AgriculturalInputHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListInputHistoryQueryHandler(AppDbContext db, ILogger<ListInputHistoryQueryHandler> logger)
{
    public async Task<Result<Page<AgriculturalInputHistoryResponse>>> HandleAsync(Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.ListInputHistory", new Dictionary<string, object?> { });
        Result<Page<AgriculturalInputHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<AgriculturalInputHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        if (!await db.Inputs.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "AgriculturalInput" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new AgriculturalInputHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<AgriculturalInputHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
