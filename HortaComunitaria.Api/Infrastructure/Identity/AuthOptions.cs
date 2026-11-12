namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Duração das credenciais e local das chaves que as protegem (seção <c>Authentication</c>).</summary>
public sealed class AuthTokenOptions
{
    /// <summary>Validade do access token. Curta, porque não pode ser revogado antes de expirar.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    /// <summary>Validade do refresh token (cookie HttpOnly). Revogável pelo security stamp.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
    /// <summary>Diretório das chaves do Data Protection, relativo ao content root. Precisa persistir entre reinícios.</summary>
    public string KeysPath { get; set; } = "App_Data/keys";
    /// <summary>Requisições por minuto e por IP nas rotas de autenticação.</summary>
    public int RateLimitPerMinute { get; set; } = 10;
}

/// <summary>Dados do primeiro administrador (seção <c>Bootstrap</c>), lidos só pelo comando <c>--bootstrap-admin</c>.</summary>
public sealed class BootstrapAdministratorOptions
{
    /// <summary>E-mail de acesso.</summary>
    public string Email { get; set; } = "";
    /// <summary>Nome de exibição.</summary>
    public string DisplayName { get; set; } = "";
    /// <summary>Senha inicial; deve ser trocada no primeiro acesso. Fornecer por variável de ambiente.</summary>
    public string Password { get; set; } = "";
}
