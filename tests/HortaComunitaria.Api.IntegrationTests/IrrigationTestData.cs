using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;

namespace HortaComunitaria.Api.IntegrationTests;

internal static class IrrigationTestData
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
    internal static string BasePath => $"/api/v1/gardens/{DemoIds.Garden}";
    internal static string ConsumptionPath => BasePath + "/indicators/water-consumption?from=2026-10-08&to=2026-10-08";
    internal static string ListPath => BasePath + "/irrigations?from=2026-10-08&to=2026-10-08";
    internal static RegisterIrrigationRequest Request(decimal? volume = 120m) =>
        new(Guid.CreateVersion7(), DemoIds.SectorOne, DemoIds.Beds.ToArray(),
            new(2026, 10, 8, 8, 0, 0, TimeSpan.FromHours(-3)),
            new(2026, 10, 8, 8, 30, 0, TimeSpan.FromHours(-3)), 30m, volume, null,
            volume is null ? null : VolumeSource.Measured, volume is null ? null : "Recipiente graduado",
            IrrigationStatus.Completed, DemoIds.Participant, null);

    internal static Task<HttpResponseMessage> PostAsync(HttpClient client, RegisterIrrigationRequest request) =>
        client.PostAsJsonAsync(BasePath + "/irrigations", request, Json);

    internal static async Task<IrrigationDetailResponse> CreateAsync(HttpClient client, RegisterIrrigationRequest request)
    {
        var response = await PostAsync(client, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<IrrigationDetailResponse>(Json))!;
    }
}

