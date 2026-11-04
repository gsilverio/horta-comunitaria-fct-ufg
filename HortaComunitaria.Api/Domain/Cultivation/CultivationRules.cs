using HortaComunitaria.Api.Common.Results;
namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Invariantes puras entre fatos e seu contexto de cultivo.</summary>
public static class CultivationRules
{
    /// <summary>Um fato agrícola precisa pertencer ao intervalo conhecido do ciclo.</summary>
    public static Error? EventDate(CultivationCycle cycle, DateOnly on) =>
        on < cycle.StartedOn || cycle.EndedOn is { } end && on > end
            ? Error.Validation("date", "Data deve pertencer ao período conhecido do ciclo.") : null;
    /// <summary>Área ocupada é positiva e não supera a configuração histórica selecionada.</summary>
    public static Error? Area(decimal occupiedAreaM2, decimal cultivableAreaM2) =>
        occupiedAreaM2 <= 0 || occupiedAreaM2 > cultivableAreaM2
            ? Error.Validation("occupiedAreaM2", "Área ocupada deve ser positiva e não superar a área cultivável vigente.") : null;
    /// <summary>Procedência conhecida não pode contradizer espécie/cultivar do ciclo.</summary>
    public static Error? Origin(CultivationCycle cycle, PropagationLot? lot) =>
        lot is not null && (lot.SpeciesId != cycle.SpeciesId || lot.CultivarId is not null && cycle.CultivarId is not null && lot.CultivarId != cycle.CultivarId)
            ? Error.Validation("propagationLotId", "Material e ciclo devem compartilhar espécie e cultivares conhecidas compatíveis.") : null;
    /// <summary>Avaliação de um plantio não pode antecipá-lo nem ultrapassar sua quantidade.</summary>
    public static Error? Assessment(Planting planting, EstablishmentAssessmentValues values) =>
        values.AssessedOn < planting.PlantedOn || values.SurvivingQuantity > planting.PlantedQuantity
            ? Error.Validation("assessment", "Avaliação não pode preceder o plantio ou superar a quantidade plantada.") : null;
    /// <summary>A mesma massa não pode ser destinada duas vezes acima do saldo confirmado.</summary>
    public static Error? Allocation(decimal? usable, decimal alreadyAllocated, decimal proposed) =>
        usable is null ? Error.Validation("usableMassKg", "Pese a massa aproveitável antes da destinação.") :
        alreadyAllocated + proposed > usable ? new("drying.insufficient_balance", "Destinação excede o saldo aproveitável da colheita.", ErrorCategory.Conflict) : null;
}
