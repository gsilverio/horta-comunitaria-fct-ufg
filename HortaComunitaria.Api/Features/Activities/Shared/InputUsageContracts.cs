using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Activities.Shared;

internal sealed record InputUsageRequest([property: JsonRequired] Guid InputId, [property: JsonRequired] decimal Quantity, [property: JsonRequired] string Unit);
internal sealed record InputUsageResponse(Guid Id, Guid InputId, decimal Quantity, string Unit);
