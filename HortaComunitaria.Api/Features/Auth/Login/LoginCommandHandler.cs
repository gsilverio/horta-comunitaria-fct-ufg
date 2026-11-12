using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Features.Auth.Login;

/// <summary>Credenciais de acesso.</summary>
/// <param name="Email">E-mail cadastrado.</param>
/// <param name="Password">Senha.</param>
internal sealed record LoginRequest(string? Email, string? Password);

/// <summary>
/// Autentica por e-mail e senha. Falhas contam para o bloqueio temporário (5 tentativas, 15 min) e recebem sempre a
/// mesma resposta; e-mail inexistente também calcula um hash para não ser reconhecido pelo tempo de resposta.
/// </summary>
internal sealed class LoginCommandHandler(UserManager<AppUser> users, SignInManager<AppUser> signIn, IPasswordHasher<AppUser> hasher,
    AuthTokenService tokens, TimeProvider clock, ILogger<LoginCommandHandler> logger)
{
    private static string? timingHash;

    public async Task<Result<IssuedTokens>> HandleAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Auth.Login");
        Result<IssuedTokens> Reject(Error error, string reason)
        {
            process.Event("LoginRejected", "Rejected", "Login recusado: {Reason}", reason);
            return process.Complete(Result<IssuedTokens>.Failure(error));
        }
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
            return Reject(Error.Validation("email", "Informe e-mail e senha."), "dados ausentes");
        if (request.Password.Length > PasswordPolicy.MaximumLength) return Reject(AuthErrors.InvalidCredentials(), "senha acima do limite");

        cancellationToken.ThrowIfCancellationRequested();
        process.Step("CheckingCredentials", "Conferindo credenciais");
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || user.IsDemonstration)
        {
            var timingUser = AppUser.Create("timing@invalid.local", "timing", UserProfile.Operator, clock.GetUtcNow());
            timingHash ??= hasher.HashPassword(timingUser, Guid.CreateVersion7().ToString());
            hasher.VerifyHashedPassword(timingUser, timingHash, request.Password);
            return Reject(AuthErrors.InvalidCredentials(), "e-mail não cadastrado");
        }
        var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (check.IsLockedOut) return Reject(AuthErrors.InvalidCredentials(), "usuário bloqueado temporariamente");
        if (!check.Succeeded) return Reject(AuthErrors.InvalidCredentials(), "senha incorreta");
        if (!user.IsActive) return Reject(AuthErrors.InvalidCredentials(), "usuário inativo");

        process.Event("UserAuthenticated", "Succeeded", "Usuário {UserId} autenticado", user.Id);
        return process.Complete(Result<IssuedTokens>.Success(await tokens.IssueAsync(user)));
    }
}
