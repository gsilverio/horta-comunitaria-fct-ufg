namespace HortaComunitaria.Api.Common.Results;

/// <summary>Classifica rejeições esperadas sem acoplar o domínio ao HTTP.</summary>
public enum ErrorCategory
{
    /// <summary>Entrada incompatível com o contrato.</summary>
    Validation,
    /// <summary>Recurso ausente no contexto solicitado.</summary>
    NotFound,
    /// <summary>Estado incompatível com a operação.</summary>
    Conflict
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
}
