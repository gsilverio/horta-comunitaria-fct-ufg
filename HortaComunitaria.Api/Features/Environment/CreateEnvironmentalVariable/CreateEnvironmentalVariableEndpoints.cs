using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.CreateEnvironmentalVariable;

internal static class CreateEnvironmentalVariableEndpoints
{
    internal static RouteGroupBuilder MapCreateEnvironmentalVariableEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/environmental-variables", async (CreateEnvironmentalVariableRequest request, CreateEnvironmentalVariableCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/environmental-variables/{value.Id}", value)))
            .WithName("Environment.CreateEnvironmentalVariable").WithTags("Ambiente")
            .WithSummary("Cadastrar variável ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalVariableResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
