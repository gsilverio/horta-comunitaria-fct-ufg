using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.ListInputHistory;

internal static class ListInputHistoryEndpoints
{
    internal static RouteGroupBuilder MapListInputHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/inputs/{id:guid}/history", async (Guid id, ListInputHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Activities.ListInputHistory").WithTags("Atividades")
            .WithSummary("Listar histórico de insumo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<AgriculturalInputHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
