using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Indicators.GetSocialEducationalParticipation;

internal static class GetSocialEducationalParticipationEndpoints
{
    internal static RouteGroupBuilder MapGetSocialEducationalParticipationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/indicators/social-educational-participation", async (Guid gardenId, string? from, string? to, string? dimension, Guid? institutionId,
            GetSocialEducationalParticipationQueryHandler handler, HttpContext context, CancellationToken cancellationToken, int page = 1, int pageSize = PaginationRequest.DefaultPageSize) =>
            (await handler.HandleAsync(gardenId, new(from, to, page, pageSize, dimension, institutionId), cancellationToken)).ToHttpResult(context))
            .RequireDatePeriod().WithName("Indicators.GetSocialEducationalParticipation").WithTags("Indicadores").WithSummary("Consultar participação social e educacional")
            .WithDescription("Ações distintas, público por evento e pessoas identificadas são medidas separadas. Período obrigatório from/to em yyyy-MM-dd; hipóteses do protótipo sujeitas à PO. Paginação não altera totais.")
            .Produces<GetSocialEducationalParticipationResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
