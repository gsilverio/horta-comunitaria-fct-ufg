using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.ListSamplingPoints;

internal static class ListSamplingPointsEndpoints
{
    internal static RouteGroupBuilder MapListSamplingPointsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/sampling-points", async (Guid gardenId, ListSamplingPointsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(gardenId, new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.ListSamplingPoints").WithTags("Ambiente")
            .WithSummary("Listar ponto de coleta").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<SamplingPointResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
