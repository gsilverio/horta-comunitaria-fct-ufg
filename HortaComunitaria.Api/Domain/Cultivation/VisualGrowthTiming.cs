using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Domain.Cultivation;

/// <summary>Prazos editoriais opcionais para ilustração; não atestam maturidade agronômica.</summary>
public static class VisualGrowthTiming
{
    /// <summary>Configuração completa ou ausente; crescimento positivo e anterior à colheita.</summary>
    public static Error? Validate(int? growthDays, int? harvestDays) =>
        growthDays is null && harvestDays is null ? null :
        growthDays is > 0 && harvestDays > growthDays ? null :
        Error.Validation("visualGrowthDays", "Informe os dois prazos: crescimento maior que zero e colheita posterior, ou deixe ambos vazios.");
}
