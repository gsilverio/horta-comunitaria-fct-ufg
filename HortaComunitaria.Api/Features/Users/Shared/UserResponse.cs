using System.Net.Mail;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Users.Shared;

/// <summary>Usuário apresentado à administração.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Email">E-mail de acesso.</param>
/// <param name="DisplayName">Nome de exibição.</param>
/// <param name="Profile">Perfil: <c>Administrator</c> ou <c>Operator</c>.</param>
/// <param name="IsActive">Contas inativas não entram; a autoria anterior é preservada.</param>
/// <param name="MustChangePassword">Senha inicial ainda não trocada.</param>
/// <param name="IsLockedOut">Bloqueio temporário por tentativas erradas em vigor.</param>
/// <param name="CreatedAt">Instante do cadastro (UTC).</param>
/// <param name="Version">Versão observada, exigida na alteração.</param>
internal sealed record UserResponse(Guid Id, string Email, string DisplayName, UserProfile Profile, bool IsActive,
    bool MustChangePassword, bool IsLockedOut, DateTimeOffset CreatedAt, string Version);

/// <summary>Projeção, snapshot de histórico e validações compartilhadas pelos slices de usuários.</summary>
internal static class UserProjection
{
    internal static UserResponse Map(AppUser user, DateTimeOffset now) => new(user.Id, user.Email ?? "", user.DisplayName, user.Profile,
        user.IsActive, user.MustChangePassword, user.LockoutEnd > now, user.CreatedAt, user.ConcurrencyStamp ?? "");

    /// <summary>Snapshot sem e-mail nem dados de credenciais.</summary>
    internal static object Snapshot(AppUser user) => new { user.DisplayName, user.Profile, user.IsActive };

    internal static Error? ValidateName(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > AppUser.DisplayNameMaxLength
            ? Error.Validation("displayName", $"Informe o nome (até {AppUser.DisplayNameMaxLength} caracteres).") : null;

    internal static Error? ValidateProfile(UserProfile? profile) =>
        profile is null || !Enum.IsDefined(profile.Value) ? Error.Validation("profile", "Selecione o perfil.") : null;

    internal static Error? ValidateEmail(string? email)
    {
        var value = email?.Trim();
        return string.IsNullOrEmpty(value) || value.Length > AppUser.EmailMaxLength || !MailAddress.TryCreate(value, out var parsed) || parsed.Address != value
            ? Error.Validation("email", "Informe um e-mail válido.") : null;
    }
}
