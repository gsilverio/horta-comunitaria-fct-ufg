using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.CreateInput;

internal static class CreateInputEndpoints
{
    internal static RouteGroupBuilder MapCreateInputEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/inputs", async (CreateInputRequest request, CreateInputCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/inputs/{value.Id}", value)))
            .WithName("Activities.CreateInput").WithTags("Atividades")
            .WithSummary("Cadastrar insumo").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<AgriculturalInputResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
