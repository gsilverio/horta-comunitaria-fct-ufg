using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
namespace HortaComunitaria.Api.Features.Activities.GetInstitution;

internal static class GetInstitutionEndpoints
{
    internal static RouteGroupBuilder MapGetInstitutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/institutions/{id:guid}", async (Guid id, GetInstitutionQueryHandler handler, HttpContext context, CancellationToken cancellationToken) => (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .WithName("Activities.GetInstitution").WithTags("Atividades")
            .WithSummary("Consultar instituição").WithDescription("Registro manual do MVP, com histórico e integridade dos vínculos; autoria atribuída pelo servidor.")
            .Produces<InstitutionResponse>(200).ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409).ProducesProblem(500);
        return group;
    }
}
