using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListHarvestHistory;

internal static class ListHarvestHistoryEndpoints
{
    internal static RouteGroupBuilder MapListHarvestHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/harvests/{id:guid}/history", async (Guid gardenId, Guid id, ListHarvestHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListHarvestHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de colheita").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<HarvestHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
