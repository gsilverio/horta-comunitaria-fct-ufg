using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Auth.Login;

internal static class LoginEndpoints
{
    internal static RouteGroupBuilder MapLoginEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/auth/login", async (LoginRequest request, LoginCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, cancellationToken)).ToTokenHttpResult(context))
            .AllowAnonymous().AllowPendingPasswordChange().RequireRateLimiting(IdentityExtensions.AuthRateLimitPolicy)
            .WithName("Auth.Login").WithTags("Autenticação")
            .WithSummary("Entrar")
            .WithDescription("Valida e-mail e senha. Devolve o access token no corpo e grava o refresh token em cookie HttpOnly. " +
                             "Após 5 falhas seguidas a conta fica bloqueada por 15 minutos; todas as falhas têm a mesma resposta.")
            .Produces<AuthTokenResponse>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(429);
        return group;
    }
}
