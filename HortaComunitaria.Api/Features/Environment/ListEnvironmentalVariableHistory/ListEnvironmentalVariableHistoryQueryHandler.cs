using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalVariableHistory;

internal sealed record EnvironmentalVariableHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListEnvironmentalVariableHistoryQueryHandler(AppDbContext db, ILogger<ListEnvironmentalVariableHistoryQueryHandler> logger)
{
    public async Task<Result<Page<EnvironmentalVariableHistoryResponse>>> HandleAsync(Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.ListEnvironmentalVariableHistory", new Dictionary<string, object?> { });
        Result<Page<EnvironmentalVariableHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<EnvironmentalVariableHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        if (!await db.EnvironmentalVariables.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "EnvironmentalVariable" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new EnvironmentalVariableHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<EnvironmentalVariableHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
