using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Gardens.ListIrrigationSectorHistory;

internal static class ListIrrigationSectorHistoryEndpoints
{
    internal static RouteGroupBuilder MapListIrrigationSectorHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/sectors/{id:guid}/history", async (Guid gardenId, Guid id, ListIrrigationSectorHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.ListIrrigationSectorHistory").WithTags("Organização")
            .WithSummary("Consultar histórico de IrrigationSector")
            .WithDescription("Cadastro rastreável do MVP; identidade preservada, sem exclusão física e sem autoria recebida do cliente.")
            .Produces<Page<IrrigationSectorHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
