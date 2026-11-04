using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListCultivationCycleHistory;

internal static class ListCultivationCycleHistoryEndpoints
{
    internal static RouteGroupBuilder MapListCultivationCycleHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/cultivation-cycles/{id:guid}/history", async (Guid gardenId, Guid id, ListCultivationCycleHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListCultivationCycleHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<CultivationCycleHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
