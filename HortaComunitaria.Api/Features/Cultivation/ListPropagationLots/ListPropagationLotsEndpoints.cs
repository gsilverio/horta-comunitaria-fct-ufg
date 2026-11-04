using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListPropagationLots;

internal static class ListPropagationLotsEndpoints
{
    internal static RouteGroupBuilder MapListPropagationLotsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/propagation-lots", async (Guid gardenId, ListPropagationLotsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(gardenId, new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListPropagationLots").WithTags("Cultivo e produção")
            .WithSummary("Listar lote de origem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<PropagationLotResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
