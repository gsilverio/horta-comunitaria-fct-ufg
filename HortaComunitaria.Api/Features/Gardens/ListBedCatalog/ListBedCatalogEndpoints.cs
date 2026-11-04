using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Features.Gardens.Shared;
namespace HortaComunitaria.Api.Features.Gardens.ListBedCatalog;

internal static class ListBedCatalogEndpoints
{
    internal static RouteGroupBuilder MapListBedCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/beds/catalog", async (Guid gardenId, ListBedCatalogQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) =>
            (await handler.HandleAsync(gardenId, new(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.ListBedCatalog").WithTags("Canteiros").WithSummary("Consultar cadastros ativos ou inativos de canteiros")
            .WithDescription("Catálogo administrativo separado da seleção histórica de canteiros elegíveis para irrigação.")
            .Produces<Page<BedResponse>>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
