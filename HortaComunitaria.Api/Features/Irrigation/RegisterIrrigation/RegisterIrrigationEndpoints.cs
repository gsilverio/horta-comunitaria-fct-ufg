using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;

namespace HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;

internal static class RegisterIrrigationEndpoints
{
    internal static RouteGroupBuilder MapRegisterIrrigationEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/irrigations", async (Guid gardenId, RegisterIrrigationRequest request,
            RegisterIrrigationCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(gardenId, request, cancellationToken);
            return result.ToHttpResult(context, response =>
            {
                var location = $"/api/v1/gardens/{gardenId}/irrigations/{response.Detail.Id}";
                context.Response.Headers.Location = location;
                return response.Created ? Results.Created(location, response.Detail) : Results.Ok(response.Detail);
            });
        })
        .WithName("Irrigation.RegisterIrrigation").WithTags("Irrigação")
        .WithSummary("Registrar irrigação manual")
        .WithDescription("Instantes exigem offset; medidas em L, L/min e minutos aceitam até três casas decimais. Repetição normalizada retorna 200, criação retorna 201.")
        .Produces<IrrigationDetailResponse>(201).Produces<IrrigationDetailResponse>()
        .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
