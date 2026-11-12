using System.Security.Claims;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Features.Auth.ChangePassword;

/// <summary>Troca da própria senha.</summary>
/// <param name="CurrentPassword">Senha atual (ou a inicial recebida da administração).</param>
/// <param name="NewPassword">Nova senha: 12+ caracteres, 4 distintos, sem senhas comuns nem e-mail/nome.</param>
internal sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

/// <summary>
/// Troca a senha do usuário autenticado e conclui a troca obrigatória do primeiro acesso. O Identity renova o
/// security stamp, revogando as demais sessões; esta sessão recebe tokens novos.
/// </summary>
internal sealed class ChangePasswordCommandHandler(UserManager<AppUser> users, AuthTokenService tokens, ILogger<ChangePasswordCommandHandler> logger)
{
    public async Task<Result<IssuedTokens>> HandleAsync(ChangePasswordRequest request, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Auth.ChangePassword");
        Result<IssuedTokens> Reject(Error error) => process.Complete(Result<IssuedTokens>.Failure(error));
        if (string.IsNullOrEmpty(request.CurrentPassword)) return Reject(Error.Validation("currentPassword", "Informe a senha atual."));
        if (string.IsNullOrEmpty(request.NewPassword)) return Reject(Error.Validation("newPassword", "Informe a nova senha."));
        if (request.NewPassword.Length > PasswordPolicy.MaximumLength || request.CurrentPassword.Length > PasswordPolicy.MaximumLength)
            return Reject(Error.Validation("newPassword", $"A senha deve ter no máximo {PasswordPolicy.MaximumLength} caracteres."));
        if (request.NewPassword == request.CurrentPassword)
            return Reject(Error.Validation("newPassword", "A nova senha deve ser diferente da atual."));

        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.GetUserAsync(principal);
        if (user is not { IsActive: true }) return Reject(AuthErrors.SessionExpired());

        process.Step("ChangingPassword", "Validando e gravando a nova senha do usuário {UserId}", user.Id);
        // Gravado junto com a nova senha; se a troca falhar, nada é persistido.
        user.CompletePasswordChange();
        var changed = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changed.Succeeded)
        {
            var field = changed.Errors.Any(x => x.Code == nameof(IdentityErrorDescriber.PasswordMismatch)) ? "currentPassword" : "newPassword";
            return Reject(Error.Validation(field, string.Join(" ", changed.Errors.Select(x => x.Description).Distinct())));
        }
        process.Event("PasswordChanged", "Succeeded", "Senha do usuário {UserId} alterada", user.Id);
        return process.Complete(Result<IssuedTokens>.Success(await tokens.IssueAsync(user)));
    }
}
