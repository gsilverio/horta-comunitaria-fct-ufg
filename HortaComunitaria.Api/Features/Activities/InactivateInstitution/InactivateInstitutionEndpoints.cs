using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.InactivateInstitution;

internal static class InactivateInstitutionEndpoints
{
    internal static RouteGroupBuilder MapInactivateInstitutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/institutions/{id:guid}/inactivation", async (Guid id, InactivateInstitutionRequest request, InactivateInstitutionCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Activities.InactivateInstitution").WithTags("Atividades")
            .WithSummary("Inativar instituição").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<InstitutionResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
