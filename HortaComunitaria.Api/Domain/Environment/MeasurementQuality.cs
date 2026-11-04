namespace HortaComunitaria.Api.Domain.Environment;
/// <summary>Qualidade informada da coleta, separada de sua validade.</summary>
public enum MeasurementQuality
{
    /// <summary>Coleta sem indicação de dúvida.</summary>
    Normal,
    /// <summary>Coleta duvidosa, apresentada sinalizada sem exclusão silenciosa.</summary>
    Questionable
}
