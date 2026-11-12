using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.UpdateInstitution;

internal static class UpdateInstitutionEndpoints
{
    internal static RouteGroupBuilder MapUpdateInstitutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/institutions/{id:guid}", async (Guid id, UpdateInstitutionRequest request, UpdateInstitutionCommandHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator().WithName("Activities.UpdateInstitution").WithTags("Atividades")
            .WithSummary("Editar instituição").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<InstitutionResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
