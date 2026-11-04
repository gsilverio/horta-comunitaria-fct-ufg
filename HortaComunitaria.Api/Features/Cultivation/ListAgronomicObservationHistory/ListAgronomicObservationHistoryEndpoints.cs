using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Cultivation.ListAgronomicObservationHistory;

internal static class ListAgronomicObservationHistoryEndpoints
{
    internal static RouteGroupBuilder MapListAgronomicObservationHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/agronomic-observations/{id:guid}/history", async (Guid gardenId, Guid id, ListAgronomicObservationHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Cultivation.ListAgronomicObservationHistory").WithTags("Cultivo e produção")
            .WithSummary("Listar histórico de observação agronômica").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<AgronomicObservationHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
