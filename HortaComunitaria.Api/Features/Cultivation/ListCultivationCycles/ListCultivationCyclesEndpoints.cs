using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListCultivationCycles;

internal static class ListCultivationCyclesEndpoints
{
    internal static RouteGroupBuilder MapListCultivationCyclesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/cultivation-cycles", async (Guid gardenId, ListCultivationCyclesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? bedId = null, Guid? speciesId = null, string? status = null) => (await handler.HandleAsync(gardenId, new ListCultivationCyclesRequest(from, to, page, pageSize, validity, bedId, speciesId, status), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Cultivation.ListCultivationCycles").WithTags("Cultivo e produção")
            .WithSummary("Listar ciclo de cultivo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<CultivationCycleResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
