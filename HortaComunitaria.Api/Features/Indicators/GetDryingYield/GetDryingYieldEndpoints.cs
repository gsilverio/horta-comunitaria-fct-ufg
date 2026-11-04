using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetDryingYield;

internal static class GetDryingYieldEndpoints
{
    internal static RouteGroupBuilder MapGetDryingYieldEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/drying-yield", async (Guid gardenId, string? from, string? to, Guid? lotId, Guid? speciesId,
            GetDryingYieldQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, lotId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetDryingYield").WithTags("Indicadores").WithSummary("Consultar rendimento da secagem")
            .WithDescription("Rendimento ponderado dos lotes concluídos; sem atribuição de massa seca por origem. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetDryingYieldResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
