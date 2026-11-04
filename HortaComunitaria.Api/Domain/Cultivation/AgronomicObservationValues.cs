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

namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Valores editáveis de AgronomicObservation, criados somente após validação de invariantes locais.</summary>
public sealed record AgronomicObservationValues
{
    private AgronomicObservationValues() { }
    /// <summary>Campo cycleId validado e normalizado.</summary>
    public Guid CycleId { get; private init; }
    /// <summary>Campo observedOn validado e normalizado.</summary>
    public DateOnly ObservedOn { get; private init; }
    /// <summary>Campo category validado e normalizado.</summary>
    public string Category { get; private init; } = "";
    /// <summary>Campo description validado e normalizado.</summary>
    public string Description { get; private init; } = "";
    /// <summary>Campo measurement validado e normalizado.</summary>
    public decimal? Measurement { get; private init; }
    /// <summary>Campo unit validado e normalizado.</summary>
    public string? Unit { get; private init; }
    /// <summary>Campo method validado e normalizado.</summary>
    public string? Method { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<AgronomicObservationValues> Create(Guid cycleId, DateOnly observedOn, string category, string description, decimal? measurement, string? unit, string? method, Guid? responsibleParticipantId)
    {
        var error = (cycleId == Guid.Empty ? Error.Validation("cycleId", "Identificador inválido.") : null) ?? (observedOn == default ? Error.Validation("observedOn", "Data obrigatória.") : null) ?? RecordRules.Text(category, "category", 120, true) ?? RecordRules.Text(description, "description", 1000, true) ?? RecordRules.Number(measurement, "measurement", signed: true) ?? RecordRules.Text(unit, "unit", 40, false) ?? RecordRules.Text(method, "method", 200, false) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null);
        if (error is not null) return Result<AgronomicObservationValues>.Failure(error);
        if (measurement is not null && (string.IsNullOrWhiteSpace(unit) || string.IsNullOrWhiteSpace(method))) return Result<AgronomicObservationValues>.Failure(Error.Validation("measurement", "Medida exige unidade e método."));
        return Result<AgronomicObservationValues>.Success(new() { CycleId = cycleId, ObservedOn = observedOn, Category = category.Trim(), Description = description.Trim(), Measurement = measurement, Unit = RecordRules.Optional(unit), Method = RecordRules.Optional(method), ResponsibleParticipantId = responsibleParticipantId });
    }
}
