using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.ListGardens;

/// <summary>Somente dados necessários à seleção; não expõe entidades persistidas.</summary>
internal sealed record ListGardensResponse(Guid Id, string Code, string Name);

internal sealed class ListGardensQueryHandler(AppDbContext db, ILogger<ListGardensQueryHandler> logger)
{
    public async Task<Result<Page<ListGardensResponse>>> HandleAsync(PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.ListGardens");
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return process.Complete(Result<Page<ListGardensResponse>>.Failure(validation.Error));
        process.Step("Querying", "Consultando hortas ativas; página {Page}, tamanho {PageSize}", pagination.Page, pagination.PageSize);
        var query = db.Gardens.AsNoTracking()
            .Where(x => x.IsActive);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new ListGardensResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);
        return process.Complete(Result<Page<ListGardensResponse>>.Success(
            new Page<ListGardensResponse>(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
