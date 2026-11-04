using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.History;

/// <summary>Motivo obrigatório de alteração, normalizado e limitado a 500 caracteres.</summary>
public sealed record ChangeReason
{
    private ChangeReason(string value) => Value = value;
    /// <summary>Texto normalizado que acompanha a auditoria, nunca os logs.</summary>
    public string Value { get; }
    /// <summary>Rejeita motivo ausente, vazio ou excessivo.</summary>
    public static Result<ChangeReason> Create(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > 500
            ? Result<ChangeReason>.Failure(Error.Validation("reason", "Informe um motivo com 1 a 500 caracteres."))
            : Result<ChangeReason>.Success(new(value.Trim()));
}
