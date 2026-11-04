using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.Irrigation;

/// <summary>Valores normalizados do evento, com invariantes independentes de HTTP e persistência.</summary>
public sealed class IrrigationValues
{
    private IrrigationValues() { }
    /// <summary>Início normalizado em UTC.</summary>
    public DateTimeOffset StartedAt { get; private init; }
    /// <summary>Fim conhecido em UTC ou informação desconhecida.</summary>
    public DateTimeOffset? EndedAt { get; private init; }
    /// <summary>Minutos ativos conhecidos, até três casas decimais.</summary>
    public decimal? ActiveDurationMinutes { get; private init; }
    /// <summary>Litros informados; zero observado é distinto de null.</summary>
    public decimal? VolumeLiters { get; private init; }
    /// <summary>Vazão informativa em litros/minuto.</summary>
    public decimal? FlowRateLitersPerMinute { get; private init; }
    /// <summary>Origem obrigatória somente quando o volume é conhecido.</summary>
    public VolumeSource? VolumeSource { get; private init; }
    /// <summary>Método normalizado de medição ou estimativa.</summary>
    public string? VolumeMethod { get; private init; }
    /// <summary>Situação operacional permitida pelo protótipo.</summary>
    public IrrigationStatus Status { get; private init; }
    /// <summary>Observação opcional, sem conteúdo nos logs.</summary>
    public string? Notes { get; private init; }

    /// <summary>Rejeita inconsistências e excesso de precisão, sem truncar valores.</summary>
    public static Result<IrrigationValues> Create(DateTimeOffset startedAt, DateTimeOffset? endedAt,
        decimal? activeDurationMinutes, decimal? volumeLiters, decimal? flowRateLitersPerMinute,
        VolumeSource? volumeSource, string? volumeMethod, IrrigationStatus status, string? notes)
    {
        if (!Enum.IsDefined(status))
            return Result<IrrigationValues>.Failure(Error.Validation("status", "Situação operacional inválida."));
        if (volumeSource is not null && !Enum.IsDefined(volumeSource.Value))
            return Result<IrrigationValues>.Failure(Error.Validation("volumeSource", "Origem do volume inválida."));
        if (endedAt < startedAt)
            return Result<IrrigationValues>.Failure(Invalid("irrigation.invalid_period", "endedAt", "Fim não pode ser anterior ao início."));
        foreach (var (field, value) in new[] { ("activeDurationMinutes", activeDurationMinutes),
            ("volumeLiters", volumeLiters), ("flowRateLitersPerMinute", flowRateLitersPerMinute) })
            if (value is not null && (value < 0 || value >= 100000000000m || ((decimal.GetBits(value.Value)[3] >> 16) & 255) > 3))
                return Result<IrrigationValues>.Failure(Invalid("irrigation.invalid_measurement", field,
                    "Medida deve ser não negativa, com até 11 dígitos inteiros e três casas decimais."));
        if (endedAt is not null && activeDurationMinutes > (endedAt.Value - startedAt).Ticks / (decimal)TimeSpan.TicksPerMinute)
            return Result<IrrigationValues>.Failure(Invalid("irrigation.invalid_measurement", "activeDurationMinutes",
                "Tempo ativo não pode superar o tempo transcorrido."));
        var method = Normalize(volumeMethod);
        var normalizedNotes = Normalize(notes);
        if (method?.Length > 200)
            return Result<IrrigationValues>.Failure(Error.Validation("volumeMethod", "Método deve conter até 200 caracteres."));
        if (normalizedNotes?.Length > 1000)
            return Result<IrrigationValues>.Failure(Error.Validation("notes", "Observações devem conter até 1000 caracteres."));
        if (volumeLiters is not null && (volumeSource is null || method is null))
            return Result<IrrigationValues>.Failure(Invalid("irrigation.invalid_measurement", "volumeLiters",
                "Volume conhecido exige origem e método."));
        if (volumeLiters is null && (volumeSource is not null || method is not null))
            return Result<IrrigationValues>.Failure(Invalid("irrigation.invalid_measurement", "volumeLiters",
                "Volume desconhecido exige origem e método não informados."));
        return Result<IrrigationValues>.Success(new()
        {
            StartedAt = startedAt.ToUniversalTime(),
            EndedAt = endedAt?.ToUniversalTime(),
            ActiveDurationMinutes = Normalize(activeDurationMinutes),
            VolumeLiters = Normalize(volumeLiters),
            FlowRateLitersPerMinute = Normalize(flowRateLitersPerMinute),
            VolumeSource = volumeSource,
            VolumeMethod = method,
            Status = status,
            Notes = normalizedNotes
        });
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static decimal? Normalize(decimal? value) => value is null ? null :
        decimal.Parse(value.Value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.CultureInfo.InvariantCulture);
    private static Error Invalid(string code, string field, string message) =>
        new(code, message, ErrorCategory.Validation, new Dictionary<string, string[]> { [field] = [message] });
}
