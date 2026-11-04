using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record ActivityInstitutionRequest([property: JsonRequired] Guid InstitutionId, string? Role);
internal sealed record ActivityInstitutionResponse(Guid InstitutionId, string? Role);
