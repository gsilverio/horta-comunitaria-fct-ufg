using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetHarvestLosses;

internal static class GetHarvestLossesEndpoints
{
    internal static RouteGroupBuilder MapGetHarvestLossesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/harvest-losses", async (Guid gardenId, string? from, string? to, Guid? cycleId, Guid? speciesId,
            GetHarvestLossesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, cycleId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetHarvestLosses").WithTags("Indicadores").WithSummary("Consultar perdas conhecidas da colheita")
            .WithDescription("Perda = fresca menos aproveitável conhecida; percentual ponderado apenas sobre eventos elegíveis. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetHarvestLossesResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
