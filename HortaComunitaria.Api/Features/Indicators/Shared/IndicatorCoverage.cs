namespace HortaComunitaria.Api.Features.Indicators.Shared;

internal enum IndicatorAvailability { NoData, Unavailable, Partial, Available }
internal static class IndicatorCoverage
{
    internal static IndicatorAvailability Availability(int total, int known) => total == 0 ? IndicatorAvailability.NoData : known == 0 ? IndicatorAvailability.Unavailable : known < total ? IndicatorAvailability.Partial : IndicatorAvailability.Available;
}
