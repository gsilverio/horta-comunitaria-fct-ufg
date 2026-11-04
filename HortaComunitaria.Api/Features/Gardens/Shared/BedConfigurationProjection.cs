using HortaComunitaria.Api.Domain.Gardens;
namespace HortaComunitaria.Api.Features.Gardens.Shared;

internal sealed record BedConfigurationResponse(Guid Id, Guid GardenId, Guid BedId, decimal? LengthMeters, decimal? WidthMeters, decimal CultivableAreaM2, DateOnly ValidFrom, DateOnly? ValidUntil);
internal static class BedConfigurationProjection
{
    internal static System.Linq.Expressions.Expression<Func<BedConfiguration, BedConfigurationResponse>> Select => x => new(x.Id, x.GardenId, x.BedId, x.LengthMeters, x.WidthMeters, x.CultivableAreaM2, x.ValidFrom, x.ValidUntil);
    internal static BedConfigurationResponse Map(BedConfiguration x) => new(x.Id, x.GardenId, x.BedId, x.LengthMeters, x.WidthMeters, x.CultivableAreaM2, x.ValidFrom, x.ValidUntil);
}
