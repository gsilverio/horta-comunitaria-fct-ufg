using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListPlantings;

internal static class ListPlantingsEndpoints
{
    internal static RouteGroupBuilder MapListPlantingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/plantings", async (Guid gardenId, ListPlantingsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? cycleId = null, string? kind = null, Guid? bedId = null, Guid? speciesId = null) => (await handler.HandleAsync(gardenId, new ListPlantingsRequest(from, to, page, pageSize, validity, cycleId, kind, bedId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Cultivation.ListPlantings").WithTags("Cultivo e produção")
            .WithSummary("Listar plantio").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<PlantingResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
