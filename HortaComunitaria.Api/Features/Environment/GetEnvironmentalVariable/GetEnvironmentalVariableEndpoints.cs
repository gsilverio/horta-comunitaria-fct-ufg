using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Environment.GetEnvironmentalVariable;

internal static class GetEnvironmentalVariableEndpoints
{
    internal static RouteGroupBuilder MapGetEnvironmentalVariableEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/environmental-variables/{id:guid}", async (Guid id, GetEnvironmentalVariableQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .WithName("Environment.GetEnvironmentalVariable").WithTags("Ambiente")
            .WithSummary("Consultar variável ambiental").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<EnvironmentalVariableResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
