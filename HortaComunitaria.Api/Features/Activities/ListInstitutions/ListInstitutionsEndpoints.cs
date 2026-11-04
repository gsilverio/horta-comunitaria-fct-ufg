using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.ListInstitutions;

internal static class ListInstitutionsEndpoints
{
    internal static RouteGroupBuilder MapListInstitutionsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/institutions", async (ListInstitutionsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.ListInstitutions").WithTags("Atividades")
            .WithSummary("Listar instituição").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<InstitutionResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
