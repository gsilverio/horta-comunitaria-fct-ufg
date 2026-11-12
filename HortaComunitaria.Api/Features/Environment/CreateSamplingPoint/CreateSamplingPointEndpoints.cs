using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.CreateSamplingPoint;

internal static class CreateSamplingPointEndpoints
{
    internal static RouteGroupBuilder MapCreateSamplingPointEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{gardenId:guid}/sampling-points", async (Guid gardenId, CreateSamplingPointRequest request, CreateSamplingPointCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(gardenId, request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/gardens/{gardenId}/sampling-points/{value.Id}", value)))
            .RequireAdministrator().WithName("Environment.CreateSamplingPoint").WithTags("Ambiente")
            .WithSummary("Cadastrar ponto de coleta").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<SamplingPointResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
