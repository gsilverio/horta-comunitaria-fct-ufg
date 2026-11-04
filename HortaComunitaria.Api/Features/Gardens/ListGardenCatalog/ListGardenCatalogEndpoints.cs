using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.ListGardenCatalog;

internal static class ListGardenCatalogEndpoints
{
    internal static RouteGroupBuilder MapListGardenCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/catalog", async (ListGardenCatalogQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) =>
            (await handler.HandleAsync(new(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.ListGardenCatalog").WithTags("Hortas")
            .WithSummary("Consultar cadastros ativos ou inativos de hortas")
            .WithDescription("Catálogo administrativo paginado, separado da seleção de referências ativas para novos registros.")
            .Produces<Page<GardenResponse>>().ProducesValidationProblem().ProducesProblem(500);
        return group;
    }
}

