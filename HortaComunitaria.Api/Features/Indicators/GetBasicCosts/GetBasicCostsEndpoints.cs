using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetBasicCosts;

internal static class GetBasicCostsEndpoints
{
    internal static RouteGroupBuilder MapGetBasicCostsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/basic-costs", async (Guid gardenId, string? from, string? to, string? category, Guid? bedId, Guid? cycleId,
            GetBasicCostsQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, category, bedId, cycleId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetBasicCosts").WithTags("Indicadores").WithSummary("Comparar custos previstos e realizados")
            .WithDescription("Naturezas separadas; ausência em uma natureza permanece null, sem rateio de custos gerais. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetBasicCostsResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
