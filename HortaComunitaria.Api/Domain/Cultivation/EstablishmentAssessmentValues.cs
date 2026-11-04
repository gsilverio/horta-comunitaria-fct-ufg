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
/// <summary>Valores editáveis de EstablishmentAssessment, criados somente após validação de invariantes locais.</summary>
public sealed record EstablishmentAssessmentValues
{
    private EstablishmentAssessmentValues() { }
    /// <summary>Campo plantingId validado e normalizado.</summary>
    public Guid PlantingId { get; private init; }
    /// <summary>Campo assessedOn validado e normalizado.</summary>
    public DateOnly AssessedOn { get; private init; }
    /// <summary>Campo survivingQuantity validado e normalizado.</summary>
    public int SurvivingQuantity { get; private init; }
    /// <summary>Campo method validado e normalizado.</summary>
    public string Method { get; private init; } = "";
    /// <summary>Campo notes validado e normalizado.</summary>
    public string? Notes { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<EstablishmentAssessmentValues> Create(Guid plantingId, DateOnly assessedOn, int survivingQuantity, string method, string? notes, Guid? responsibleParticipantId)
    {
        var error = (plantingId == Guid.Empty ? Error.Validation("plantingId", "Identificador inválido.") : null) ?? (assessedOn == default ? Error.Validation("assessedOn", "Data obrigatória.") : null) ?? RecordRules.Text(method, "method", 200, true) ?? RecordRules.Text(notes, "notes", 1000, false) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null);
        if (error is not null) return Result<EstablishmentAssessmentValues>.Failure(error);
        if (survivingQuantity < 0) return Result<EstablishmentAssessmentValues>.Failure(Error.Validation("survivingQuantity", "Sobreviventes não podem ser negativos."));
        return Result<EstablishmentAssessmentValues>.Success(new() { PlantingId = plantingId, AssessedOn = assessedOn, SurvivingQuantity = survivingQuantity, Method = method.Trim(), Notes = RecordRules.Optional(notes), ResponsibleParticipantId = responsibleParticipantId });
    }
}
