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
/// <summary>Valores editáveis de Planting, criados somente após validação de invariantes locais.</summary>
public sealed record PlantingValues
{
    private PlantingValues() { }
    /// <summary>Campo cycleId validado e normalizado.</summary>
    public Guid CycleId { get; private init; }
    /// <summary>Campo propagationLotId validado e normalizado.</summary>
    public Guid? PropagationLotId { get; private init; }
    /// <summary>Campo replacedPlantingId validado e normalizado.</summary>
    public Guid? ReplacedPlantingId { get; private init; }
    /// <summary>Campo plantedOn validado e normalizado.</summary>
    public DateOnly PlantedOn { get; private init; }
    /// <summary>Campo kind validado e normalizado.</summary>
    public PlantingKind Kind { get; private init; }
    /// <summary>Campo plantedQuantity validado e normalizado.</summary>
    public int PlantedQuantity { get; private init; }
    /// <summary>Campo plantSpacingCm validado e normalizado.</summary>
    public decimal? PlantSpacingCm { get; private init; }
    /// <summary>Campo rowSpacingCm validado e normalizado.</summary>
    public decimal? RowSpacingCm { get; private init; }
    /// <summary>Campo responsibleParticipantId validado e normalizado.</summary>
    public Guid? ResponsibleParticipantId { get; private init; }
    /// <summary>Valida formato, precisão, estados e informação desconhecida do registro.</summary>
    public static Result<PlantingValues> Create(Guid cycleId, Guid? propagationLotId, Guid? replacedPlantingId, DateOnly plantedOn, PlantingKind kind, int plantedQuantity, decimal? plantSpacingCm, decimal? rowSpacingCm, Guid? responsibleParticipantId)
    {
        var error = (cycleId == Guid.Empty ? Error.Validation("cycleId", "Identificador inválido.") : null) ?? (propagationLotId == Guid.Empty ? Error.Validation("propagationLotId", "Identificador inválido.") : null) ?? (replacedPlantingId == Guid.Empty ? Error.Validation("replacedPlantingId", "Identificador inválido.") : null) ?? (plantedOn == default ? Error.Validation("plantedOn", "Data obrigatória.") : null) ?? (!Enum.IsDefined(kind) ? Error.Validation("kind", "Opção indefinida.") : null) ?? RecordRules.Number(plantSpacingCm, "plantSpacingCm", 3) ?? RecordRules.Number(rowSpacingCm, "rowSpacingCm", 3) ?? (responsibleParticipantId == Guid.Empty ? Error.Validation("responsibleParticipantId", "Identificador inválido.") : null);
        if (error is not null) return Result<PlantingValues>.Failure(error);
        if (plantedQuantity <= 0 || plantSpacingCm <= 0 || rowSpacingCm <= 0 || kind == PlantingKind.Initial && replacedPlantingId is not null) return Result<PlantingValues>.Failure(Error.Validation("planting", "Quantidade e espaçamentos conhecidos devem ser positivos; vínculo de reposição exige Replacement."));
        return Result<PlantingValues>.Success(new() { CycleId = cycleId, PropagationLotId = propagationLotId, ReplacedPlantingId = replacedPlantingId, PlantedOn = plantedOn, Kind = kind, PlantedQuantity = plantedQuantity, PlantSpacingCm = plantSpacingCm, RowSpacingCm = rowSpacingCm, ResponsibleParticipantId = responsibleParticipantId });
    }
}
