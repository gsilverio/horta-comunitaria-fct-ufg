using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetSoilMoisture;

internal static class GetSoilMoistureEndpoints
{
    internal static RouteGroupBuilder MapGetSoilMoistureEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/soil-moisture", async (Guid gardenId, string? from, string? to, Guid? pointId, string? unit, string? method,
            GetSoilMoistureQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, pointId, unit, method), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetSoilMoisture").WithTags("Indicadores").WithSummary("Consultar observações de umidade do solo")
            .WithDescription("Observações e qualidade separadas por ponto, unidade e método. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetSoilMoistureResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
