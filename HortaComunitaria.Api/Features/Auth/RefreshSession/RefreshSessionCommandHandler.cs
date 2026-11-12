using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;

namespace HortaComunitaria.Api.Features.Auth.RefreshSession;

/// <summary>
/// Renova a sessão pelo refresh token do cookie: emite novo access token e substitui o refresh token. Recusa token
/// expirado, revogado (logout, troca de senha, mudança de perfil ou desativação) ou de usuário inativo.
/// </summary>
internal sealed class RefreshSessionCommandHandler(AuthTokenService tokens, ILogger<RefreshSessionCommandHandler> logger)
{
    public async Task<Result<IssuedTokens>> HandleAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Auth.RefreshSession");
        cancellationToken.ThrowIfCancellationRequested();
        var user = await tokens.ValidateRefreshTokenAsync(refreshToken);
        if (user is null) return process.Complete(Result<IssuedTokens>.Failure(AuthErrors.SessionExpired()));
        return process.Complete(Result<IssuedTokens>.Success(await tokens.IssueAsync(user)));
    }
}
