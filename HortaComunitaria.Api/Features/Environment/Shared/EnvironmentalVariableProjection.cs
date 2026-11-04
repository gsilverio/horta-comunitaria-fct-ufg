using HortaComunitaria.Api.Domain.Environment;
namespace HortaComunitaria.Api.Features.Environment.Shared;

internal sealed record EnvironmentalVariableResponse(Guid Id, string Code, string Name, string Quantity, string ReferenceUnit, string? Description, bool IsActive, int Version, DateTimeOffset? UpdatedAt, bool IsSoilMoisture);
internal static class EnvironmentalVariableProjection
{
    internal static System.Linq.Expressions.Expression<Func<EnvironmentalVariable, EnvironmentalVariableResponse>> Select => x => new(x.Id, x.Code, x.Name, x.Quantity, x.ReferenceUnit, x.Description, x.IsActive, x.Version, x.UpdatedAt, x.IsSoilMoisture);
    internal static EnvironmentalVariableResponse Map(EnvironmentalVariable x) => new(x.Id, x.Code, x.Name, x.Quantity, x.ReferenceUnit, x.Description, x.IsActive, x.Version, x.UpdatedAt, x.IsSoilMoisture);
}
