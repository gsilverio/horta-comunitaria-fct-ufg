using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListCultivarHistory;

internal static class ListCultivarHistoryEndpoints
{
    internal static RouteGroupBuilder MapListCultivarHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/species/{speciesId:guid}/cultivars/{id:guid}/history", async (Guid speciesId, Guid id, ListCultivarHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(speciesId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListCultivarHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de cultivar").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<CultivarHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
