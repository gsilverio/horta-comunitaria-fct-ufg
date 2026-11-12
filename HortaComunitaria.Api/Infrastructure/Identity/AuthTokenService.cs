using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Credenciais emitidas para uma sessão. O refresh token só sai da API dentro do cookie HttpOnly.</summary>
/// <param name="AccessToken">Token enviado em <c>Authorization: Bearer</c>.</param>
/// <param name="AccessTokenExpiresIn">Segundos até o access token expirar.</param>
/// <param name="RefreshToken">Token de renovação (gravado apenas em cookie).</param>
/// <param name="RefreshTokenLifetime">Validade do refresh token a partir da emissão.</param>
public sealed record IssuedTokens(string AccessToken, long AccessTokenExpiresIn, string RefreshToken, TimeSpan RefreshTokenLifetime);

/// <summary>
/// Emite e valida os tokens do esquema bearer nativo do Identity com os mesmos protetores do <c>BearerTokenHandler</c>:
/// o access token emitido aqui é aceito pela autenticação padrão, e access/refresh têm propósitos distintos.
/// </summary>
internal sealed class AuthTokenService(IOptionsMonitor<BearerTokenOptions> bearerOptions, SignInManager<AppUser> signInManager, TimeProvider clock)
{
    /// <summary>Gera um par novo com as claims atuais do usuário (inclui perfil e security stamp).</summary>
    public async Task<IssuedTokens> IssueAsync(AppUser user)
    {
        var options = bearerOptions.Get(IdentityConstants.BearerScheme);
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var now = clock.GetUtcNow();
        var accessToken = options.BearerTokenProtector.Protect(new AuthenticationTicket(principal,
            new AuthenticationProperties { ExpiresUtc = now + options.BearerTokenExpiration }, $"{IdentityConstants.BearerScheme}:AccessToken"));
        var refreshToken = options.RefreshTokenProtector.Protect(new AuthenticationTicket(principal,
            new AuthenticationProperties { ExpiresUtc = now + options.RefreshTokenExpiration }, $"{IdentityConstants.BearerScheme}:RefreshToken"));
        return new IssuedTokens(accessToken, (long)options.BearerTokenExpiration.TotalSeconds, refreshToken, options.RefreshTokenExpiration);
    }

    /// <summary>Valida assinatura, expiração, security stamp e situação; retorna <c>null</c> sem distinguir o motivo.</summary>
    public async Task<AppUser?> ValidateRefreshTokenAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        var options = bearerOptions.Get(IdentityConstants.BearerScheme);
        var ticket = options.RefreshTokenProtector.Unprotect(refreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expiresUtc || clock.GetUtcNow() >= expiresUtc) return null;
        var user = await signInManager.ValidateSecurityStampAsync(ticket.Principal);
        return user is { IsActive: true } ? user : null;
    }
}

/// <summary>
/// Cookie do refresh token: <c>HttpOnly</c> contém XSS, <c>SameSite=Strict</c> impede envio por outros sites e o
/// caminho restrito evita que acompanhe as demais requisições.
/// </summary>
internal static class RefreshTokenCookie
{
    /// <summary>Nome; o prefixo <c>__Secure-</c> obriga o navegador a só aceitá-lo com <c>Secure</c>.</summary>
    public const string Name = "__Secure-horta.refresh";
    /// <summary>Único caminho para o qual o navegador envia o cookie.</summary>
    public const string Path = "/api/v1/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];
    public static void Write(HttpResponse response, IssuedTokens tokens) =>
        response.Cookies.Append(Name, tokens.RefreshToken, Options(tokens.RefreshTokenLifetime));
    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

    // Max-Age relativo não depende de o relógio do navegador coincidir com o do servidor.
    private static CookieOptions Options(TimeSpan? maxAge) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = Path, MaxAge = maxAge, IsEssential = true
    };
}
