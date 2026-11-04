using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.ListInputs;

internal static class ListInputsEndpoints
{
    internal static RouteGroupBuilder MapListInputsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/inputs", async (ListInputsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) => (await handler.HandleAsync(new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.ListInputs").WithTags("Atividades")
            .WithSummary("Listar insumo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<AgriculturalInputResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
