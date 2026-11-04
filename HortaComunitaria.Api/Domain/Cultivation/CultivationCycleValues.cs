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
/// <summary>Valores editáveis de CultivationCycle, criados somente após validação de invariantes locais.</summary>
public sealed record CultivationCycleValues
{
    private CultivationCycleValues() { }
    /// <summary>Campo bedId validado e normalizado.</summary>
    public Guid BedId { get; private init; }
    /// <summary>Campo speciesId validado e normalizado.</summary>
    public Guid SpeciesId { get; private init; }
    /// <summary>Campo cultivarId validado e normalizado.</summary>
    public Guid? CultivarId { get; private init; }
    /// <summary>Campo startedOn validado e normalizado.</summary>
    public DateOnly StartedOn { get; private init; }
    /// <summary>Campo occupiedAreaM2 validado e normalizado.</summary>
    public decimal OccupiedAreaM2 { get; private init; }
    /// <summary>Campo endedOn validado e normalizado.</summary>
    public DateOnly? EndedOn { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<CultivationCycleValues> Create(Guid bedId, Guid speciesId, Guid? cultivarId, DateOnly startedOn, decimal occupiedAreaM2, DateOnly? endedOn, Guid? responsibleParticipantId)
    {
        var error = (bedId == Guid.Empty ? Error.Validation("bedId", "Identificador inválido.") : null) ?? (speciesId == Guid.Empty ? Error.Validation("speciesId", "Identificador inválido.") : null) ?? (cultivarId == Guid.Empty ? Error.Validation("cultivarId", "Identificador inválido.") : null) ?? (startedOn == default ? Error.Validation("startedOn", "Data obrigatória.") : null) ?? RecordRules.Number(occupiedAreaM2, "occupiedAreaM2", 3) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null);
        if (error is not null) return Result<CultivationCycleValues>.Failure(error);
        if (occupiedAreaM2 <= 0 || endedOn < startedOn) return Result<CultivationCycleValues>.Failure(Error.Validation("cycle", "Área deve ser positiva e fim não anterior ao início."));
        return Result<CultivationCycleValues>.Success(new() { BedId = bedId, SpeciesId = speciesId, CultivarId = cultivarId, StartedOn = startedOn, OccupiedAreaM2 = occupiedAreaM2, EndedOn = endedOn, ResponsibleParticipantId = responsibleParticipantId });
    }
}
