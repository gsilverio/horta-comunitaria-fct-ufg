using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Gardens.ListGardenHistory;

internal static class ListGardenHistoryEndpoints
{
    internal static RouteGroupBuilder MapListGardenHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/history", async (Guid id, ListGardenHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.ListGardenHistory").WithTags("Organização")
            .WithSummary("Consultar histórico de Garden")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<Page<GardenHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
