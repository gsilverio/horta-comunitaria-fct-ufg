using System.Security.Claims;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Auth.GetCurrentUser;

internal static class GetCurrentUserEndpoints
{
    internal static RouteGroupBuilder MapGetCurrentUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/auth/me", async (ClaimsPrincipal user, GetCurrentUserQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(user, cancellationToken)).ToHttpResult(context))
            .AllowPendingPasswordChange()
            .WithName("Auth.GetCurrentUser").WithTags("Autenticação")
            .WithSummary("Consultar usuário atual")
            .WithDescription("Dados e perfil do usuário autenticado, incluindo se a troca da senha inicial está pendente.")
            .Produces<CurrentUserResponse>().ProducesProblem(401);
        return group;
    }
}
