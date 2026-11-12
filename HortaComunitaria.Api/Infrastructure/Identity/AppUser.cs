using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Perfil de acesso único de cada usuário. Labels PostgreSQL em <c>user_profile</c>.</summary>
public enum UserProfile
{
    /// <summary>Registra, corrige e invalida fatos de campo; consulta todos os dados.</summary>
    Operator,
    /// <summary>Além do Operador, mantém estrutura, catálogos e usuários.</summary>
    Administrator
}

/// <summary>
/// Usuário autenticável e autor dos registros. Fica na infraestrutura porque herda do ASP.NET Core Identity;
/// o domínio referencia apenas o identificador do autor.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>Tamanho máximo do nome de exibição.</summary>
    public const int DisplayNameMaxLength = 120;
    /// <summary>Tamanho máximo do e-mail (mesmo limite das colunas do Identity).</summary>
    public const int EmailMaxLength = 256;

    private AppUser() { }

    /// <summary>Nome apresentado na interface e no histórico.</summary>
    public string DisplayName { get; private set; } = "";
    /// <summary>Usuário inativo não entra, não renova sessão e não grava; a autoria anterior é preservada.</summary>
    public bool IsActive { get; private set; }
    /// <summary>Perfil de acesso.</summary>
    public UserProfile Profile { get; private set; }
    /// <summary>Enquanto verdadeiro, o servidor só aceita consultar a sessão, trocar a senha e sair.</summary>
    public bool MustChangePassword { get; private set; }
    /// <summary>Autor técnico do protótipo sem autenticação; sem credenciais e apenas histórico.</summary>
    public bool IsDemonstration { get; private set; }
    /// <summary>Instante do cadastro (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Cria um usuário ativo que deve trocar a senha inicial no primeiro acesso. Dados já validados.</summary>
    public static AppUser Create(string email, string displayName, UserProfile profile, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(),
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        DisplayName = displayName,
        Profile = profile,
        IsActive = true,
        MustChangePassword = true,
        CreatedAt = now.ToUniversalTime()
    };

    /// <summary>Registra que a senha definida pelo próprio usuário substituiu a inicial.</summary>
    public void CompletePasswordChange() => MustChangePassword = false;

    /// <summary>Altera nome, perfil e situação definidos pela administração.</summary>
    public void UpdateAccess(string displayName, UserProfile profile, bool isActive)
    {
        DisplayName = displayName;
        Profile = profile;
        IsActive = isActive;
    }
}
