using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetProductivity;

internal static class GetProductivityEndpoints
{
    internal static RouteGroupBuilder MapGetProductivityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/productivity", async (Guid gardenId, string? from, string? to, Guid? cycleId, Guid? speciesId,
            GetProductivityQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, cycleId, speciesId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetProductivity").WithTags("Indicadores").WithSummary("Consultar produtividade por ciclo")
            .WithDescription("Área congelada de cada ciclo, usada uma vez; massas em kg, área em m². Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetProductivityResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
