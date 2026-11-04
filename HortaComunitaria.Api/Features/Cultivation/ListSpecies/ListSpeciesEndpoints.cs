using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListSpecies;

internal static class ListSpeciesEndpoints
{
    internal static RouteGroupBuilder MapListSpeciesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species", async (ListSpeciesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListSpecies").WithTags("Cultivo e produção")
            .WithSummary("Listar espécie").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<SpeciesResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
