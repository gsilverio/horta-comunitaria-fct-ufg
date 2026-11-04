using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalVariables;

internal sealed class ListEnvironmentalVariablesQueryHandler(AppDbContext db, ILogger<ListEnvironmentalVariablesQueryHandler> logger)
{
    public async Task<Result<Page<EnvironmentalVariableResponse>>> HandleAsync(PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.ListEnvironmentalVariables", new Dictionary<string, object?> { });
        Result<Page<EnvironmentalVariableResponse>> Reject(Error error) => process.Complete(Result<Page<EnvironmentalVariableResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var query = db.EnvironmentalVariables.AsNoTracking().Where(x => (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(EnvironmentalVariableProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<EnvironmentalVariableResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
