namespace HortaComunitaria.Api.Common.Results;
/// <summary>Enum de query usa somente o nome documentado, sem representações numéricas.</summary>
public static class EnumFilter
{
    /// <summary>Interpreta filtro opcional, preservando ausência e rejeitando valores indefinidos.</summary>
    public static Result<OptionalEnum<T>> Parse<T>(string? text, string field) where T : struct, Enum =>
        text is null ? Result<OptionalEnum<T>>.Success(new(null)) :
        Enum.GetNames<T>().Contains(text, StringComparer.Ordinal) && Enum.TryParse<T>(text, out var value)
            ? Result<OptionalEnum<T>>.Success(new(value)) : Result<OptionalEnum<T>>.Failure(Error.Validation(field, "Use um dos nomes de enum documentados."));
}

/// <summary>Filtro interpretado com ausência explícita, sem sucesso com payload null.</summary>
public readonly record struct OptionalEnum<T>(T? Value) where T : struct, Enum;
