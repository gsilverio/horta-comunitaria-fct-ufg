using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.ListActivityHistory;

internal static class ListActivityHistoryEndpoints
{
    internal static RouteGroupBuilder MapListActivityHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/activities/{id:guid}/history", async (Guid gardenId, Guid id, ListActivityHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Activities.ListActivityHistory").WithTags("Atividades")
            .WithSummary("Listar histórico de atividade").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<ActivityHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
