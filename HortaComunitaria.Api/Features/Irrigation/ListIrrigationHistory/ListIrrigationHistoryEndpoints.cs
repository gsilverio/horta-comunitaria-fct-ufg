using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Irrigation.ListIrrigationHistory;

internal static class ListIrrigationHistoryEndpoints
{
    internal static RouteGroupBuilder MapListIrrigationHistoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/irrigations/{id:guid}/history", async (Guid gardenId, Guid id,
            ListIrrigationHistoryQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
            int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, id, new(page, pageSize), cancellationToken)).ToHttpResult(context))
            .WithName("Irrigation.ListIrrigationHistory").WithTags("Irrigação")
            .WithSummary("Consultar histórico de uma irrigação")
            .WithDescription("Inclui Created/Corrected/Invalidated, motivo, autoria demonstrativa e snapshots antes/depois. Acessível para registro invalidado; ordenação por instante/id decrescente. Histórico não compõe o consumo.")
            .Produces<Page<IrrigationHistoryResponse>>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
