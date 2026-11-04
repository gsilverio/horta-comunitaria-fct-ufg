using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetOperationalOccurrences;

internal static class GetOperationalOccurrencesEndpoints
{
    internal static RouteGroupBuilder MapGetOperationalOccurrencesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/operational-occurrences", async (Guid gardenId, string? from, string? to, string? category, string? status,
            GetOperationalOccurrencesQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, category, status), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetOperationalOccurrences").WithTags("Indicadores").WithSummary("Consultar ocorrências operacionais")
            .WithDescription("Ocorrências distintas pela data de abertura e estado atual; intervenções não multiplicam a contagem. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetOperationalOccurrencesResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
