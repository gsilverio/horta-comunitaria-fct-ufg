using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Activities.ListParticipantCatalog;

internal static class ListParticipantCatalogEndpoints
{
    internal static RouteGroupBuilder MapListParticipantCatalogEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/participants/catalog", async (Guid gardenId, ListParticipantCatalogQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) =>
            (await handler.HandleAsync(gardenId, new(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.ListParticipantCatalog").WithTags("Atividades")
            .WithSummary("Consultar cadastros ativos ou inativos de participantes")
            .WithDescription("Catálogo administrativo paginado, separado da seleção de referências ativas para novos registros.")
            .Produces<Page<ParticipantResponse>>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}

