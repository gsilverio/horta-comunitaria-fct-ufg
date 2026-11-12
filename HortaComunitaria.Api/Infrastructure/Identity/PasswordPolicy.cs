using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Limites de senha compartilhados pelas opções do Identity e pela validação das requisições.</summary>
public static class PasswordPolicy
{
    /// <summary>Tamanho mínimo (comprimento protege mais que regras de símbolos).</summary>
    public const int MinimumLength = 12;
    /// <summary>Tamanho máximo aceito na entrada; evita custo de hash abusivo.</summary>
    public const int MaximumLength = 128;
    /// <summary>Quantidade mínima de caracteres distintos.</summary>
    public const int RequiredUniqueChars = 4;
}

/// <summary>Recusa senhas muito comuns e senhas que contenham o e-mail ou o nome do usuário.</summary>
internal sealed class CommonPasswordValidator : IPasswordValidator<AppUser>
{
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "1234567890123", "12345678901234", "123456123456", "123123123123", "111111111111",
        "000000000000", "abcdefghijkl", "abc123abc123", "qwertyuiopas", "qwerty123456", "qwertyqwerty",
        "password1234", "passwordpassword", "password123!", "senha1234567", "senhasenha12", "senha@123456",
        "mudar@123456", "mudarsenha123", "trocar123456", "admin1234567", "administrador", "administrador1",
        "iloveyou1234", "brasil123456", "hortacomunitaria", "horta1234567", "hortafctufg123", "apoema123456"
    };

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string? password)
    {
        if (string.IsNullOrEmpty(password)) return Task.FromResult(IdentityResult.Success);
        var errors = new List<IdentityError>();
        if (CommonPasswords.Contains(password))
            errors.Add(new IdentityError { Code = "CommonPassword", Description = "Esta senha é comum demais. Escolha outra." });
        if (ContainsPersonalData(password, user))
            errors.Add(new IdentityError { Code = "PasswordContainsUserData", Description = "A senha não pode conter o e-mail ou o nome." });
        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]));
    }

    private static bool ContainsPersonalData(string password, AppUser user)
    {
        string?[] parts = [user.Email?.Split('@')[0], .. user.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)];
        return parts.Any(part => part is { Length: >= 4 } && password.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>Mensagens do Identity em pt-BR para as regras usadas pela aplicação.</summary>
internal sealed class PtBrIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Não foi possível concluir a operação.");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "Senha atual incorreta.");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"A senha deve ter pelo menos {length} caracteres.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes.");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), "Já existe um usuário com este e-mail.");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), "Já existe um usuário com este e-mail.");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), "E-mail inválido.");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "E-mail inválido.");
    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure), "O usuário foi alterado por outra operação. Recarregue e tente novamente.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
