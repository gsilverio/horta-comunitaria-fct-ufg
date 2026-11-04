using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.ListInstitutionHistory;

internal static class ListInstitutionHistoryEndpoints
{
    internal static RouteGroupBuilder MapListInstitutionHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/institutions/{id:guid}/history", async (Guid id, ListInstitutionHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Activities.ListInstitutionHistory").WithTags("Atividades")
            .WithSummary("Listar histórico de instituição").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<InstitutionHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
