using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.ListInputs;

internal sealed class ListInputsQueryHandler(AppDbContext db, ILogger<ListInputsQueryHandler> logger)
{
    public async Task<Result<Page<AgriculturalInputResponse>>> HandleAsync(PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.ListInputs", new Dictionary<string, object?> { });
        Result<Page<AgriculturalInputResponse>> Reject(Error error) => process.Complete(Result<Page<AgriculturalInputResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var query = db.Inputs.AsNoTracking().Where(x => (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(AgriculturalInputProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<AgriculturalInputResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
