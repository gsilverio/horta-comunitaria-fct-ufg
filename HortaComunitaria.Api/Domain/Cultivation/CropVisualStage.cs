namespace HortaComunitaria.Api.Domain.Cultivation;

/// <summary>Estados da ilustração, derivados de fatos e estimativas explicitamente configuradas.</summary>
public enum CropVisualStage
{
    /// <summary>Ciclo ainda sem plantio inicial elegível.</summary>
    AwaitingPlanting,
    /// <summary>Plantio registrado; ainda sem prazo configurado para estimar evolução.</summary>
    Planted,
    /// <summary>Ilustração inicial antes do prazo intermediário.</summary>
    Seedling,
    /// <summary>Prazo intermediário atingido, sem comprovar crescimento real.</summary>
    Growing,
    /// <summary>Prazo de colheita atingido; previsão visual, não maturidade confirmada.</summary>
    HarvestExpected,
    /// <summary>Há colheita positiva registrada, sem presumir remoção total das plantas.</summary>
    HarvestRecorded,
    /// <summary>Plantios em datas diferentes ou reposições; não assumir uma fase uniforme.</summary>
    Mixed
}
