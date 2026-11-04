using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.Costs;

namespace HortaComunitaria.Api.Domain.Environment;
/// <summary>Valores editáveis de EnvironmentalMeasurement, criados somente após validação de invariantes locais.</summary>
public sealed record EnvironmentalMeasurementValues
{
    private EnvironmentalMeasurementValues() { }
    /// <summary>Campo samplingPointId validado e normalizado.</summary>
    public Guid SamplingPointId { get; private init; }
    /// <summary>Campo variableId validado e normalizado.</summary>
    public Guid VariableId { get; private init; }
    /// <summary>Campo collectedAt validado e normalizado.</summary>
    public DateTimeOffset CollectedAt { get; private init; }
    /// <summary>Campo value validado e normalizado.</summary>
    public decimal Value { get; private init; }
    /// <summary>Campo unit validado e normalizado.</summary>
    public string Unit { get; private init; } = "";
    /// <summary>Campo method validado e normalizado.</summary>
    public string Method { get; private init; } = "";
    /// <summary>Campo quality validado e normalizado.</summary>
    public MeasurementQuality Quality { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Campo notes validado e normalizado.</summary>
    public string? Notes { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<EnvironmentalMeasurementValues> Create(Guid samplingPointId, Guid variableId, DateTimeOffset collectedAt, decimal value, string unit, string method, MeasurementQuality quality, Guid? responsibleParticipantId, string? notes)
    {
        var error = (samplingPointId == Guid.Empty ? Error.Validation("samplingPointId", "Identificador inválido.") : null) ?? (variableId == Guid.Empty ? Error.Validation("variableId", "Identificador inválido.") : null) ?? (collectedAt == default ? Error.Validation("collectedAt", "Instante obrigatório.") : null) ?? RecordRules.Number(value, "value", signed: true) ?? RecordRules.Text(unit, "unit", 40, true) ?? RecordRules.Text(method, "method", 200, true) ?? (!Enum.IsDefined(quality) ? Error.Validation("quality", "Opção indefinida.") : null) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null) ?? RecordRules.Text(notes, "notes", 1000, false);
        if (error is not null) return Result<EnvironmentalMeasurementValues>.Failure(error);

        return Result<EnvironmentalMeasurementValues>.Success(new() { SamplingPointId = samplingPointId, VariableId = variableId, CollectedAt = collectedAt.ToUniversalTime(), Value = value, Unit = unit.Trim(), Method = method.Trim(), Quality = quality, ResponsibleParticipantId = responsibleParticipantId, Notes = RecordRules.Optional(notes) });
    }
}
