namespace HortaComunitaria.Api.Domain.Indicators;

/// <summary>Aritmética decimal das taxas propostas para o protótipo.</summary>
public static class IndicatorMath
{
    /// <summary>Denominador nulo em valor impede taxa; arredonda somente o percentual exibido.</summary>
    public static decimal? Percentage(decimal numerator, decimal denominator) => denominator <= 0 ? null :
        Math.Round(numerator / denominator * 100m, 2, MidpointRounding.AwayFromZero);
}
