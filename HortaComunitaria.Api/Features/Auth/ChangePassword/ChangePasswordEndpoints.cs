using System.Security.Claims;
using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Auth.ChangePassword;

internal static class ChangePasswordEndpoints
{
    internal static RouteGroupBuilder MapChangePasswordEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/auth/change-password", async (ChangePasswordRequest request, ClaimsPrincipal user, HttpContext context,
                ChangePasswordCommandHandler handler, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, user, cancellationToken)).ToTokenHttpResult(context))
            .AllowPendingPasswordChange().RequireRateLimiting(IdentityExtensions.AuthRateLimitPolicy)
            .WithName("Auth.ChangePassword").WithTags("Autenticação")
            .WithSummary("Trocar senha")
            .WithDescription("Troca a própria senha (obrigatório no primeiro acesso). Encerra as outras sessões e devolve tokens novos para a sessão atual.")
            .Produces<AuthTokenResponse>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(429);
        return group;
    }
}
