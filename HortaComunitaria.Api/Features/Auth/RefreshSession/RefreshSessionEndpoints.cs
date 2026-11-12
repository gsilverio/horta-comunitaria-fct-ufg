using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Auth.RefreshSession;

internal static class RefreshSessionEndpoints
{
    internal static RouteGroupBuilder MapRefreshSessionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/auth/refresh", async (HttpContext context, RefreshSessionCommandHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(RefreshTokenCookie.Read(context.Request), cancellationToken)).ToTokenHttpResult(context))
            .AllowAnonymous().AllowPendingPasswordChange().RequireRateLimiting(IdentityExtensions.AuthRateLimitPolicy)
            .WithName("Auth.RefreshSession").WithTags("Autenticação")
            .WithSummary("Renovar sessão")
            .WithDescription("Usa o refresh token do cookie HttpOnly para emitir novo access token e substituir o cookie. " +
                             "Em falha, o cookie é removido e é preciso entrar novamente.")
            .Produces<AuthTokenResponse>().ProducesProblem(401).ProducesProblem(429);
        return group;
    }
}
