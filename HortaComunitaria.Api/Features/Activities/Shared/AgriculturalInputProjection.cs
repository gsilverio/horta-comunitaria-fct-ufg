using HortaComunitaria.Api.Domain.Activities;
namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record AgriculturalInputResponse(Guid Id, string Name, string? Category, string ReferenceUnit, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class AgriculturalInputProjection
{
    internal static System.Linq.Expressions.Expression<Func<AgriculturalInput, AgriculturalInputResponse>> Select => x => new(x.Id, x.Name, x.Category, x.ReferenceUnit, x.IsActive, x.Version, x.UpdatedAt);
    internal static AgriculturalInputResponse Map(AgriculturalInput x) => new(x.Id, x.Name, x.Category, x.ReferenceUnit, x.IsActive, x.Version, x.UpdatedAt);
}
