using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetWorkHours;

internal static class GetWorkHoursEndpoints
{
    internal static RouteGroupBuilder MapGetWorkHoursEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/work-hours", async (Guid gardenId, string? from, string? to, Guid? participantId, string? category,
            GetWorkHoursQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, participantId, category), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetWorkHours").WithTags("Indicadores").WithSummary("Consultar horas individuais conhecidas")
            .WithDescription("Soma de horas individuais antes de vínculos de insumos ou instituições; lista vazia indica cobertura insuficiente. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetWorkHoursResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
