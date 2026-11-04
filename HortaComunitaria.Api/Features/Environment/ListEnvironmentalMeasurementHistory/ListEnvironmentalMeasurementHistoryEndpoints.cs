using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalMeasurementHistory;

internal static class ListEnvironmentalMeasurementHistoryEndpoints
{
    internal static RouteGroupBuilder MapListEnvironmentalMeasurementHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/environmental-measurements/{id:guid}/history", async (Guid gardenId, Guid id, ListEnvironmentalMeasurementHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) => (await handler.HandleAsync(gardenId, id, new PaginationRequest(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Environment.ListEnvironmentalMeasurementHistory").WithTags("Ambiente")
            .WithSummary("Listar histórico de medição ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<EnvironmentalMeasurementHistoryResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
