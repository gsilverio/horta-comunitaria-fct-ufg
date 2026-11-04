using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Irrigation.InvalidateIrrigation;

internal static class InvalidateIrrigationEndpoints
{
    internal static RouteGroupBuilder MapInvalidateIrrigationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/irrigations/{id:guid}/invalidation", async (Guid gardenId, Guid id,
            InvalidateIrrigationRequest request, InvalidateIrrigationCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(gardenId, id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Irrigation.InvalidateIrrigation").WithTags("Irrigação")
            .WithSummary("Invalidar irrigação preservando o histórico")
            .WithDescription("Exige expectedVersion e motivo com 1–500 caracteres. Preserva valores/canteiros/autoria; evento sai do consumo e da listagem padrão. Versão antiga ou registro já invalidado: 409.")
            .Produces<IrrigationDetailResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
