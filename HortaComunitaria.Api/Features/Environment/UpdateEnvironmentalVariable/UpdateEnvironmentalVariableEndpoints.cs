using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.UpdateEnvironmentalVariable;

internal static class UpdateEnvironmentalVariableEndpoints
{
    internal static RouteGroupBuilder MapUpdateEnvironmentalVariableEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/environmental-variables/{id:guid}", async (Guid id, UpdateEnvironmentalVariableRequest request, UpdateEnvironmentalVariableCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.UpdateEnvironmentalVariable").WithTags("Ambiente")
            .WithSummary("Editar variável ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalVariableResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
