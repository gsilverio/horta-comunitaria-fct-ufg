using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Irrigation.GetIrrigation;

internal static class GetIrrigationEndpoints
{
    internal static RouteGroupBuilder MapGetIrrigationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{gardenId:guid}/irrigations/{id:guid}", async (Guid gardenId, Guid id,
            GetIrrigationQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, cancellationToken)).ToHttpResult(context))
            .WithName("Irrigation.GetIrrigation").WithTags("Irrigação")
            .WithSummary("Consultar detalhe da irrigação")
            .WithDescription("Preserva o contexto histórico e consulta também fatos invalidados; autoria demonstrativa é explícita.")
            .Produces<IrrigationDetailResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(500);
        return group;
    }
}
