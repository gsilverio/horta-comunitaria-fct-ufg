using System.Net;
using System.Net.Http.Json;
using HortaComunitaria.Api.Features.Irrigation.CorrectIrrigation;
using HortaComunitaria.Api.Features.Irrigation.InvalidateIrrigation;
using HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;
using HortaComunitaria.Api.Features.Irrigation.Shared;
using static HortaComunitaria.Api.IntegrationTests.IrrigationTestData;

namespace HortaComunitaria.Api.IntegrationTests;

internal static class IrrigationChangesTestData
{
    internal static string Path(Guid id) => BasePath + "/irrigations/" + id;
    internal static CorrectIrrigationRequest Correction(RegisterIrrigationRequest original, decimal? volume = 100m, int version = 1) =>
        new(original.SectorId, original.BedIds, original.StartedAt, original.EndedAt, original.ActiveDurationMinutes,
            volume, original.FlowRateLitersPerMinute, volume is null ? null : original.VolumeSource,
            volume is null ? null : original.VolumeMethod, original.Status, original.ResponsibleParticipantId, original.Notes,
            version, "Correção de coleta");
    internal static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid id, CorrectIrrigationRequest request) =>
        client.PutAsJsonAsync(Path(id), request, Json);
    internal static Task<HttpResponseMessage> InvalidateAsync(HttpClient client, Guid id, int version, string reason = "Registro duplicado") =>
        client.PostAsJsonAsync(Path(id) + "/invalidation", new InvalidateIrrigationRequest(version, reason), Json);
    internal static async Task<IrrigationDetailResponse> DetailAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<IrrigationDetailResponse>(Json))!;
    }
}
