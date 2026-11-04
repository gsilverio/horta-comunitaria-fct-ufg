using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListHarvests;

internal static class ListHarvestsEndpoints
{
    internal static RouteGroupBuilder MapListHarvestsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/harvests", async (Guid gardenId, ListHarvestsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? cycleId = null, Guid? speciesId = null) => (await handler.HandleAsync(gardenId, new ListHarvestsRequest(from, to, page, pageSize, validity, cycleId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Cultivation.ListHarvests").WithTags("Cultivo e produção")
            .WithSummary("Listar colheita").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<HarvestResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
