using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.Identity;

/// <summary>Autor técnico demonstrativo sem credenciais, perfis ou permissões.</summary>
public sealed class User
{
    private User() { }
    /// <summary>Identidade estável do cadastro.</summary>
    public Guid Id { get; private set; }
    /// <summary>Identificador externo da autoria demonstrativa.</summary>
    public string AuthenticationIdentifier { get; private set; } = "";
    /// <summary>Nome demonstrativo apresentado às pessoas.</summary>
    public string DisplayName { get; private set; } = "";
    /// <summary>Disponibilidade do cadastro para uso.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Cria somente um cadastro válido com textos normalizados.</summary>
    public static Result<User> Create(string authenticationIdentifier, string displayName, bool isActive = true, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(authenticationIdentifier) || authenticationIdentifier.Trim().Length > 200)
            return Result<User>.Failure(Error.Validation("authenticationIdentifier", "Referência obrigatória ou inválida."));
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 120)
            return Result<User>.Failure(Error.Validation("displayName", "Nome deve conter de 1 a 120 caracteres."));
        if (id == Guid.Empty) return Result<User>.Failure(Error.Validation("id", "Identificador inválido."));
        return Result<User>.Success(new User
        {
            Id = id ?? Guid.CreateVersion7(),
            AuthenticationIdentifier = authenticationIdentifier.Trim(),
            DisplayName = displayName.Trim(),
            IsActive = isActive
        });
    }
}
