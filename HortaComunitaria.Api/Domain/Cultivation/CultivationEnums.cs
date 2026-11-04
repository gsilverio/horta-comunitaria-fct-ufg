namespace HortaComunitaria.Api.Domain.Cultivation;
/// <summary>Material conhecido, sem tratar sementes como mudas no pegamento.</summary>
public enum MaterialType
{
    /// <summary>Muda elegível no indicador de pegamento.</summary>
    Seedling,
    /// <summary>Semente, sem equiparar germinação a pegamento.</summary>
    Seed,
    /// <summary>Material de estaquia.</summary>
    Cutting,
    /// <summary>Outro material identificado pela equipe.</summary>
    Other
}
/// <summary>Situação operacional do ciclo, separada da validade.</summary>
public enum CultivationStatus
{
    /// <summary>Acompanhamento sem encerramento informado.</summary>
    InProgress,
    /// <summary>Acompanhamento encerrado em data conhecida.</summary>
    Closed
}
/// <summary>Reposição mantém sua própria quantidade e avaliação.</summary>
public enum PlantingKind
{
    /// <summary>Plantio sem constituir reposição.</summary>
    Initial,
    /// <summary>Reposição distinta do plantio anterior.</summary>
    Replacement
}
/// <summary>Pesagem final conhecida distingue secagem concluída.</summary>
public enum DryingStatus
{
    /// <summary>Pesagem final ainda não coletada.</summary>
    InProgress,
    /// <summary>Fim e massa seca informados.</summary>
    Completed
}
