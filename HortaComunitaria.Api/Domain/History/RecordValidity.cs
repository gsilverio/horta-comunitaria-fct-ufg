namespace HortaComunitaria.Api.Domain.History;

/// <summary>Elegibilidade do fato para indicadores, separada de sua situação operacional.</summary>
public enum RecordValidity
{
    /// <summary>Fato elegível.</summary>
    Valid,
    /// <summary>Fato preservado, mas excluído dos indicadores.</summary>
    Invalid
}

/// <summary>Operações preservadas no histórico do fato.</summary>
public enum AuditOperation
{
    /// <summary>Inclusão inicial.</summary>
    Created,
    /// <summary>Correção com motivo.</summary>
    Corrected,
    /// <summary>Invalidação com motivo.</summary>
    Invalidated
}
