using System.Security.Claims;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Features.Auth.Logout;

/// <summary>
/// Encerra as sessões renovando o security stamp, o que revoga todos os refresh tokens do usuário.
/// Access tokens já emitidos expiram sozinhos em poucos minutos.
/// </summary>
internal sealed class LogoutCommandHandler(AuthTokenService tokens, UserManager<AppUser> users, ILogger<LogoutCommandHandler> logger)
{
    public async Task<Result<Unit>> HandleAsync(string? refreshToken, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Auth.Logout");
        cancellationToken.ThrowIfCancellationRequested();
        var user = await tokens.ValidateRefreshTokenAsync(refreshToken)
                   ?? (principal.Identity?.IsAuthenticated == true ? await users.GetUserAsync(principal) : null);
        if (user is not null)
        {
            await users.UpdateSecurityStampAsync(user);
            process.Event("SessionsRevoked", "Succeeded", "Sessões do usuário {UserId} encerradas", user.Id);
        }
        return process.Complete(Result<Unit>.Success(new Unit()));
    }
}
