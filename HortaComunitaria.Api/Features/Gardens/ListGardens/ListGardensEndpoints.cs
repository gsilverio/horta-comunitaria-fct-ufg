using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.ListGardens;

internal static class ListGardensEndpoints
{
    internal static RouteGroupBuilder MapListGardensEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("", async (
            ListGardensQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
        {
            var result = await handler.HandleAsync(new PaginationRequest(page, pageSize),
                cancellationToken);
            return result.ToHttpResult(context);
        })
        .WithName("Gardens.ListGardens").WithTags("Hortas")
        .WithSummary("Listar hortas ativas")
        .WithDescription("Consulta paginada em ordem estável, com somente cadastros ativos.")
        .Produces<Page<ListGardensResponse>>()
        .ProducesValidationProblem().ProducesProblem(500);
        return group;
    }
}
