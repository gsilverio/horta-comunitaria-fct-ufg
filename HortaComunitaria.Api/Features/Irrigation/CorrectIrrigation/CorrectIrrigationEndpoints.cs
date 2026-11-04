using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Irrigation.CorrectIrrigation;

internal static class CorrectIrrigationEndpoints
{
    internal static RouteGroupBuilder MapCorrectIrrigationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{gardenId:guid}/irrigations/{id:guid}", async (Guid gardenId, Guid id, CorrectIrrigationRequest request,
            CorrectIrrigationCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Irrigation.CorrectIrrigation").WithTags("Irrigação")
            .WithSummary("Corrigir irrigação com histórico")
            .WithDescription("Envie o formulário completo, null explícito para campos não coletados, expectedVersion e motivo de 1–500 caracteres. Medidas em L, L/min e minutos: até três casas decimais. Conflito retorna 409; correção sem mudança não avança versão/histórico.")
            .Produces<IrrigationDetailResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
