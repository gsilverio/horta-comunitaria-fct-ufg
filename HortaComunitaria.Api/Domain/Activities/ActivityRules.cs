using HortaComunitaria.Api.Common.Results;
namespace HortaComunitaria.Api.Domain.Activities;

/// <summary>Compatibilidade entre os contextos conhecidos da intervenção e de sua ocorrência.</summary>
public static class ActivityRules
{
    /// <summary>Não inventa contexto geral; rejeita somente contradições entre referências conhecidas.</summary>
    public static Error? OccurrenceContext(Guid? activityBed, Guid? activitySector, Guid? activityCycle,
        Guid? occurrenceBed, Guid? occurrenceSector, Guid? occurrenceCycle, bool occurrenceBedBelongsToActivitySector) =>
        occurrenceBed is not null && activityBed is not null && occurrenceBed != activityBed ||
        occurrenceSector is not null && (activityBed is not null || activitySector is not null) && occurrenceSector != activitySector ||
        occurrenceCycle is not null && activityCycle is not null && occurrenceCycle != activityCycle ||
        !occurrenceBedBelongsToActivitySector
            ? Error.Validation("occurrenceId", "Contexto da atividade é incompatível com a ocorrência selecionada.") : null;
}
