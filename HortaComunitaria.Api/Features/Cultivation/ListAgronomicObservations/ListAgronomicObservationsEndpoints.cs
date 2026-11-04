using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListAgronomicObservations;

internal static class ListAgronomicObservationsEndpoints
{
    internal static RouteGroupBuilder MapListAgronomicObservationsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/agronomic-observations", async (Guid gardenId, ListAgronomicObservationsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? cycleId = null, string? category = null, Guid? bedId = null, Guid? speciesId = null) => (await handler.HandleAsync(gardenId, new ListAgronomicObservationsRequest(from, to, page, pageSize, validity, cycleId, category, bedId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Cultivation.ListAgronomicObservations").WithTags("Cultivo e produção")
            .WithSummary("Listar observação agronômica").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<AgronomicObservationResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
