using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListSpeciesHistory;

internal static class ListSpeciesHistoryEndpoints
{
    internal static RouteGroupBuilder MapListSpeciesHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species/{id:guid}/history", async (Guid id, ListSpeciesHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListSpeciesHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de espécie").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<SpeciesHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
