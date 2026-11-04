using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.InactivateEnvironmentalVariable;

internal static class InactivateEnvironmentalVariableEndpoints
{
    internal static RouteGroupBuilder MapInactivateEnvironmentalVariableEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/environmental-variables/{id:guid}/inactivation", async (Guid id, InactivateEnvironmentalVariableRequest request, InactivateEnvironmentalVariableCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.InactivateEnvironmentalVariable").WithTags("Ambiente")
            .WithSummary("Inativar variável ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalVariableResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
