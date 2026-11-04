using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListCultivars;

internal static class ListCultivarsEndpoints
{
    internal static RouteGroupBuilder MapListCultivarsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species/{speciesId:guid}/cultivars", async (Guid speciesId, ListCultivarsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(speciesId, new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListCultivars").WithTags("Cultivo e produção")
            .WithSummary("Listar cultivar").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<CultivarResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
