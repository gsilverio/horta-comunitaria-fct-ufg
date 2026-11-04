using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record ActivityParticipantRequest([property: JsonRequired] Guid ParticipantId, string? Role, decimal? WorkHours, bool CountsAsAudience);
internal sealed record ActivityParticipantResponse(Guid ParticipantId, string? Role, decimal? WorkHours, bool CountsAsAudience);
