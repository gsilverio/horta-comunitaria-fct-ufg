using HortaComunitaria.Api.Common.Pagination;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalMeasurements;

internal static class ListEnvironmentalMeasurementsEndpoints
{
    internal static RouteGroupBuilder MapListEnvironmentalMeasurementsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/environmental-measurements", async (Guid gardenId, ListEnvironmentalMeasurementsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, string? from = null, string? to = null, int page = 1, int pageSize = PaginationRequest.DefaultPageSize, string? validity = null, Guid? pointId = null, Guid? variableId = null, string? unit = null, string? method = null, string? quality = null) => (await handler.HandleAsync(gardenId, new ListEnvironmentalMeasurementsRequest(from, to, page, pageSize, validity, pointId, variableId, unit, method, quality), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Environment.ListEnvironmentalMeasurements").WithTags("Ambiente")
            .WithSummary("Listar medição ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<Page<EnvironmentalMeasurementResponse>>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
