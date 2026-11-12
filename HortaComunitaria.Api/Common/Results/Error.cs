namespace HortaComunitaria.Api.Common.Results;

/// <summary>Classifica rejeições esperadas sem acoplar o domínio ao HTTP.</summary>
public enum ErrorCategory
{
    /// <summary>Entrada incompatível com o contrato.</summary>
    Validation,
    /// <summary>Recurso ausente no contexto solicitado.</summary>
    NotFound,
    /// <summary>Estado incompatível com a operação.</summary>
    Conflict,
    /// <summary>Credenciais ou sessão inválidas; exige nova autenticação.</summary>
    Unauthorized
}

/// <summary>Erro com código estável, mensagem pública e detalhes opcionais por campo.</summary>
public sealed record Error(string Code, string Message, ErrorCategory Category,
    IDictionary<string, string[]>? Fields = null)
{
    /// <summary>Produz a rejeição padronizada de entrada.</summary>
    public static Error Validation(string field, string message) =>
        new("validation.invalid_request", message, ErrorCategory.Validation,
            new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>Produz ausência sem revelar recursos de outra horta.</summary>
    public static Error NotFound() => new("resource.not_found",
        "Recurso não encontrado nesta horta.", ErrorCategory.NotFound);

    /// <summary>Produz rejeição de conflito com código estável.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorCategory.Conflict);

    /// <summary>Produz rejeição de credenciais/sessão sem revelar o motivo exato.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorCategory.Unauthorized);
}
