using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.History;

/// <summary>Validações comuns de formato, precisão e controle de alterações do protótipo.</summary>
public static class RecordRules
{
    /// <summary>Normaliza informação opcional sem transformar desconhecido em texto vazio.</summary>
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    /// <summary>Valida texto obrigatório ou opcional antes da persistência.</summary>
    public static Error? Text(string? value, string field, int maximum, bool required = false) =>
        required && string.IsNullOrWhiteSpace(value) || value?.Trim().Length > maximum
            ? Error.Validation(field, $"Informe texto {(required ? "obrigatório " : "")}de até {maximum} caracteres.") : null;
    /// <summary>Rejeita valores que seriam arredondados ou excederiam numeric(14, escala).</summary>
    public static Error? Number(decimal? value, string field, int scale = 3, bool positive = false, bool signed = false) =>
        value is { } number && ((!signed && (positive ? number <= 0 : number < 0)) ||
            decimal.Round(number, scale) != number || Math.Abs(number) >= (scale == 2 ? 1_000_000_000_000m : 100_000_000_000m))
            ? Error.Validation(field, $"Valor incompatível com a unidade e precisão de {scale} casas.") : null;
    /// <summary>Impede sobrescrita concorrente e alteração de informação invalidada/inativa.</summary>
    public static Error? Change(int expectedVersion, int currentVersion, bool eligible = true) =>
        expectedVersion < 1 ? Error.Validation("expectedVersion", "Versão esperada deve ser positiva.") :
        expectedVersion != currentVersion ? new("record.version_conflict", "O registro foi alterado; recarregue e revise o rascunho.", ErrorCategory.Conflict) :
        !eligible ? new("record.inactive", "O registro está inativo ou invalidado.", ErrorCategory.Conflict) : null;
    /// <summary>Dependentes válidos exigem revisão explícita, sem cascata.</summary>
    public static Error HasDependents(string message) => new("record.has_dependents", message, ErrorCategory.Conflict);
}
