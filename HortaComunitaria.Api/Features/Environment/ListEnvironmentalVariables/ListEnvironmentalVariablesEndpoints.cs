using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalVariables;

internal static class ListEnvironmentalVariablesEndpoints
{
    internal static RouteGroupBuilder MapListEnvironmentalVariablesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/environmental-variables", async (ListEnvironmentalVariablesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.ListEnvironmentalVariables").WithTags("Ambiente")
            .WithSummary("Listar variável ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<EnvironmentalVariableResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
