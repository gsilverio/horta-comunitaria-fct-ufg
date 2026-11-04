using HortaComunitaria.Api.Domain.Activities;
namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record InstitutionResponse(Guid Id, string Name, string? Acronym, string? Type, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class InstitutionProjection
{
    internal static System.Linq.Expressions.Expression<Func<Institution, InstitutionResponse>> Select => x => new(x.Id, x.Name, x.Acronym, x.Type, x.IsActive, x.Version, x.UpdatedAt);
    internal static InstitutionResponse Map(Institution x) => new(x.Id, x.Name, x.Acronym, x.Type, x.IsActive, x.Version, x.UpdatedAt);
}
