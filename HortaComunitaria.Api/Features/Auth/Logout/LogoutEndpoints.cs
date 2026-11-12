using System.Security.Claims;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Auth.Logout;

internal static class LogoutEndpoints
{
    internal static RouteGroupBuilder MapLogoutEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/auth/logout", async (HttpContext context, ClaimsPrincipal user, LogoutCommandHandler handler, CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(RefreshTokenCookie.Read(context.Request), user, cancellationToken);
                RefreshTokenCookie.Delete(context.Response);
                return result.ToHttpResult(context, _ => Results.NoContent());
            })
            .AllowAnonymous().AllowPendingPasswordChange().RequireRateLimiting(IdentityExtensions.AuthRateLimitPolicy)
            .WithName("Auth.Logout").WithTags("Autenticação")
            .WithSummary("Sair")
            .WithDescription("Remove o cookie de renovação e revoga todas as sessões do usuário. Funciona com o access token expirado; responde 204.")
            .Produces(204);
        return group;
    }
}
