namespace HortaComunitaria.Api.Domain.Cultivation;

/// <summary>Resolve somente a fase ilustrativa, preservando ausência, mistura e a precedência de fatos.</summary>
public static class CropVisualAppearance
{
    /// <summary>Colheita real prevalece sobre previsão; ausência de prazo não produz crescimento automático.</summary>
    public static CropVisualStage Resolve(DateOnly on, DateOnly? firstPlantedOn, bool hasHarvest, bool hasMixedDates,
        bool hasReplacements, int? growthDays, int? harvestDays)
    {
        if (hasHarvest) return CropVisualStage.HarvestRecorded;
        if (firstPlantedOn is null) return CropVisualStage.AwaitingPlanting;
        if (hasMixedDates || hasReplacements) return CropVisualStage.Mixed;
        if (growthDays is null || harvestDays is null) return CropVisualStage.Planted;
        var age = on.DayNumber - firstPlantedOn.Value.DayNumber;
        return age >= harvestDays ? CropVisualStage.HarvestExpected : age >= growthDays ? CropVisualStage.Growing : CropVisualStage.Seedling;
    }
}
