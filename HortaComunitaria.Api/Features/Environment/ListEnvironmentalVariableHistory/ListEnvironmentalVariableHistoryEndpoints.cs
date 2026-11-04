using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalVariableHistory;

internal static class ListEnvironmentalVariableHistoryEndpoints
{
    internal static RouteGroupBuilder MapListEnvironmentalVariableHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/environmental-variables/{id:guid}/history", async (Guid id, ListEnvironmentalVariableHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Environment.ListEnvironmentalVariableHistory").WithTags("Ambiente")
            .WithSummary("Listar histórico de variável ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<EnvironmentalVariableHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
