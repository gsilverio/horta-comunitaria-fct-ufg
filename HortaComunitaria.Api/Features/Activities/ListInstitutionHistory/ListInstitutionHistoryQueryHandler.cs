using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.ListInstitutionHistory;

internal sealed record InstitutionHistoryResponse(Guid Id, AuditOperation Operation, string? Reason, Guid UserId, DateTimeOffset OccurredAt, string? Before, string After);
internal sealed class ListInstitutionHistoryQueryHandler(AppDbContext db, ILogger<ListInstitutionHistoryQueryHandler> logger)
{
    public async Task<Result<Page<InstitutionHistoryResponse>>> HandleAsync(Guid id, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.ListInstitutionHistory", new Dictionary<string, object?> { });
        Result<Page<InstitutionHistoryResponse>> Reject(Error error) => process.Complete(Result<Page<InstitutionHistoryResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        if (!await db.Institutions.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken)) return Reject(Error.NotFound());
        var query = db.AuditEntries.AsNoTracking().Where(x => x.EntityType == "Institution" && x.EntityKey == id);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new InstitutionHistoryResponse(x.Id, x.Operation, x.Reason, x.UserId, x.OccurredAt, x.Before, x.After)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<InstitutionHistoryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
