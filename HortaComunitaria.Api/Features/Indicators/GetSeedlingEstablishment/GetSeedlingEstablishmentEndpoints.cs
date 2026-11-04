using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetSeedlingEstablishment;

internal static class GetSeedlingEstablishmentEndpoints
{
    internal static RouteGroupBuilder MapGetSeedlingEstablishmentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/seedling-establishment", async (Guid gardenId, string? from, string? to, Guid? speciesId, string? kind, string? method,
            GetSeedlingEstablishmentQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, speciesId, kind, method), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetSeedlingEstablishment").WithTags("Indicadores").WithSummary("Consultar pegamento de mudas")
            .WithDescription("Última avaliação por plantio de muda; métodos e reposições separados, sem somar avaliações sucessivas. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetSeedlingEstablishmentResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
