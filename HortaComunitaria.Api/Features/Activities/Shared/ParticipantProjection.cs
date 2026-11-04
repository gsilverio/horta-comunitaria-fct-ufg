using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;
namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record ParticipantResponse(Guid Id, Guid GardenId, string DisplayName, string? Category, bool IsActive, int Version, DateTimeOffset? UpdatedAt);
internal static class ParticipantProjection
{
    internal static System.Linq.Expressions.Expression<Func<Participant, ParticipantResponse>> Select => x => new(x.Id, x.GardenId, x.DisplayName, x.Category, x.IsActive, x.Version, x.UpdatedAt);
    internal static ParticipantResponse Map(Participant x) => new(x.Id, x.GardenId, x.DisplayName, x.Category, x.IsActive, x.Version, x.UpdatedAt);
}
