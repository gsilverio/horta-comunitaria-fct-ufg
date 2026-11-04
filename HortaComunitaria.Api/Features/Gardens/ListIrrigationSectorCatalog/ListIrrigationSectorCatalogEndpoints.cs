using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Gardens.ListIrrigationSectorCatalog;

internal static class ListIrrigationSectorCatalogEndpoints
{
    internal static RouteGroupBuilder MapListIrrigationSectorCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/sectors/catalog", async (Guid gardenId, ListIrrigationSectorCatalogQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) =>
            (await handler.HandleAsync(gardenId, new(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Gardens.ListIrrigationSectorCatalog").WithTags("Hortas")
            .WithSummary("Consultar cadastros ativos ou inativos de setores")
            .WithDescription("Catálogo administrativo paginado, separado da seleção de referências ativas para novos registros.")
            .Produces<Page<IrrigationSectorResponse>>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}

