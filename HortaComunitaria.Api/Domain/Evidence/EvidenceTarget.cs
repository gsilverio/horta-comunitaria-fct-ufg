namespace HortaComunitaria.Api.Domain.Evidence;

/// <summary>Registro ao qual a fotografia está vinculada. Cada valor corresponde a uma FK concreta em <see cref="EvidenceFile"/>.</summary>
public enum EvidenceTarget
{
    /// <summary>Canteiro.</summary>
    Bed,
    /// <summary>Ciclo de cultivo.</summary>
    CultivationCycle,
    /// <summary>Plantio ou reposição.</summary>
    Planting,
    /// <summary>Avaliação de pegamento.</summary>
    EstablishmentAssessment,
    /// <summary>Observação agronômica (crescimento, pragas, doenças, deficiências, rebrote).</summary>
    AgronomicObservation,
    /// <summary>Colheita.</summary>
    Harvest,
    /// <summary>Lote de secagem.</summary>
    DryingLot,
    /// <summary>Atividade realizada.</summary>
    Activity,
    /// <summary>Ocorrência operacional (intercorrência de campo).</summary>
    Occurrence
}
