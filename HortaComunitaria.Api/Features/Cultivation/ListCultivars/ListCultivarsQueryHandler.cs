using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.ListCultivars;

internal sealed class ListCultivarsQueryHandler(AppDbContext db, ILogger<ListCultivarsQueryHandler> logger)
{
    public async Task<Result<Page<CultivarResponse>>> HandleAsync(Guid speciesId, PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListCultivars", new Dictionary<string, object?> { ["SpeciesId"] = speciesId });
        Result<Page<CultivarResponse>> Reject(Error error) => process.Complete(Result<Page<CultivarResponse>>.Failure(error));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        if (!await db.Species.AsNoTracking().AnyAsync(x => x.Id == speciesId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando cadastro e registros históricos preservados");
        var query = db.Cultivars.AsNoTracking().Where(x => x.SpeciesId == speciesId && (isActive == null || x.IsActive == isActive));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(CultivarProjection.Select).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<CultivarResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
