namespace HortaComunitaria.Api.Domain.Occurrences;
/// <summary>Estado atual do acompanhamento da ocorrência.</summary>
public enum OccurrenceStatus
{
    /// <summary>Problema registrado, aguardando acompanhamento.</summary>
    Open,
    /// <summary>Providências em andamento.</summary>
    InProgress,
    /// <summary>Resolução registrada com instante e descrição.</summary>
    Resolved
}
