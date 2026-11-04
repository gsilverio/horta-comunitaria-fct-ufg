using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.Shared;

internal sealed record DryingItemRequest([property: JsonRequired] Guid HarvestId, [property: JsonRequired] decimal AllocatedMassKg);
internal sealed record DryingItemResponse(Guid HarvestId, decimal AllocatedMassKg);
