using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListDryingLots;

internal static class ListDryingLotsEndpoints
{
    internal static RouteGroupBuilder MapListDryingLotsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/drying-lots", async (Guid gardenId, ListDryingLotsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, string? status = null) => (await handler.HandleAsync(gardenId, new ListDryingLotsRequest(from, to, page, pageSize, validity, status), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Cultivation.ListDryingLots").WithTags("Cultivo e produção")
            .WithSummary("Listar lote de secagem").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<DryingLotResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
