using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.CreateInstitution;

internal static class CreateInstitutionEndpoints
{
    internal static RouteGroupBuilder MapCreateInstitutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/institutions", async (CreateInstitutionRequest request, CreateInstitutionCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/institutions/{value.Id}", value)))
            .RequireAdministrator().WithName("Activities.CreateInstitution").WithTags("Atividades")
            .WithSummary("Cadastrar instituição").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<InstitutionResponse>(201).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
