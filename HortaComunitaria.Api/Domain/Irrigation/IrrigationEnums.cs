namespace HortaComunitaria.Api.Domain.Irrigation;

/// <summary>Situação operacional de um evento encerrado; independente da validade do registro.</summary>
public enum IrrigationStatus
{
    /// <summary>Execução concluída.</summary>
    Completed,
    /// <summary>Execução interrompida, cujo volume conhecido continua elegível.</summary>
    Interrupted
}

/// <summary>Origem do volume informado; não calcula volume a partir de vazão.</summary>
public enum VolumeSource
{
    /// <summary>Volume obtido por medição.</summary>
    Measured,
    /// <summary>Volume obtido por estimativa declarada.</summary>
    Estimated
}
