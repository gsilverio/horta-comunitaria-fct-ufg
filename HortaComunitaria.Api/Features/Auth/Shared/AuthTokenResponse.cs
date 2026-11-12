using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Auth.Shared;

/// <summary>Access token devolvido ao cliente. O refresh token nunca aparece no corpo: vai no cookie HttpOnly.</summary>
/// <param name="AccessToken">Enviar em <c>Authorization: Bearer {accessToken}</c>; manter apenas em memória.</param>
/// <param name="TokenType">Sempre <c>Bearer</c>.</param>
/// <param name="ExpiresIn">Segundos até expirar; depois disso, chamar <c>POST /api/v1/auth/refresh</c>.</param>
internal sealed record AuthTokenResponse(string AccessToken, string TokenType, long ExpiresIn);

/// <summary>Rejeições públicas da autenticação, propositalmente genéricas.</summary>
internal static class AuthErrors
{
    /// <summary>Única resposta para usuário inexistente, inativo, bloqueado ou senha errada.</summary>
    internal static Error InvalidCredentials() => Error.Unauthorized("auth.invalid_credentials", "E-mail ou senha inválidos.");
    /// <summary>Refresh token ausente, expirado, revogado ou de usuário inativo.</summary>
    internal static Error SessionExpired() => Error.Unauthorized("auth.session_expired", "Sessão expirada. Entre novamente.");
}

/// <summary>Tradução das credenciais emitidas em resposta HTTP.</summary>
internal static class AuthTokenHttpExtensions
{
    /// <summary>Sucesso grava o cookie de renovação e devolve o access token; 401 remove o cookie.</summary>
    internal static IResult ToTokenHttpResult(this Result<IssuedTokens> result, HttpContext context)
    {
        if (result.IsSuccess) RefreshTokenCookie.Write(context.Response, result.Value);
        else if (result.Error.Category == ErrorCategory.Unauthorized) RefreshTokenCookie.Delete(context.Response);
        return result.ToHttpResult(context, tokens => Results.Ok(new AuthTokenResponse(tokens.AccessToken, "Bearer", tokens.AccessTokenExpiresIn)));
    }
}
