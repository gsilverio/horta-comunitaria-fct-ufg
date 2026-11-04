using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListPlantingHistory;

internal static class ListPlantingHistoryEndpoints
{
    internal static RouteGroupBuilder MapListPlantingHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/plantings/{id:guid}/history", async (Guid gardenId, Guid id, ListPlantingHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListPlantingHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de plantio").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<PlantingHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
