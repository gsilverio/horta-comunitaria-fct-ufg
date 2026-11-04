using System.Text.Json;
using System.Text.Json.Serialization;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Infrastructure.Time;
using IrrigationRecord = HortaComunitaria.Api.Domain.Irrigation.Irrigation;

namespace HortaComunitaria.Api.Features.Irrigation.Shared;

/// <summary>Contexto histórico de um canteiro, sem medidas próprias.</summary>
internal sealed record IrrigationBedSnapshot(Guid BedId, Guid AssignmentId);
/// <summary>Estado auditável do fato; não contém chave/payload interno de repetição.</summary>
internal sealed record IrrigationSnapshot(Guid Id, Guid GardenId, Guid SectorId, DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt, decimal? ActiveDurationMinutes, decimal? VolumeLiters, decimal? FlowRateLitersPerMinute,
    VolumeSource? VolumeSource, string? VolumeMethod, IrrigationStatus Status, Guid? ResponsibleParticipantId,
    string? Notes, RecordValidity Validity, int Version, Guid RecordedByUserId, DateTimeOffset RecordedAt,
    DateTimeOffset? UpdatedAt, IReadOnlyList<IrrigationBedSnapshot> Beds);

internal static class IrrigationSnapshots
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
    internal static string Capture(IrrigationRecord irrigation) => JsonSerializer.Serialize(new IrrigationSnapshot(
        irrigation.Id, irrigation.GardenId, irrigation.IrrigationSectorId, InstantPrecision.ForPersistence(irrigation.StartedAt), InstantPrecision.ForPersistence(irrigation.EndedAt),
        irrigation.ActiveDurationMinutes, irrigation.VolumeLiters, irrigation.FlowRateLitersPerMinute,
        irrigation.VolumeSource, irrigation.VolumeMethod, irrigation.Status, irrigation.ResponsibleParticipantId,
        irrigation.Notes, irrigation.Validity, irrigation.Version, irrigation.RecordedByUserId, InstantPrecision.ForPersistence(irrigation.RecordedAt),
        InstantPrecision.ForPersistence(irrigation.UpdatedAt), irrigation.Beds.OrderBy(x => x.BedId).Select(x =>
            new IrrigationBedSnapshot(x.BedId, x.BedSectorAssignmentId)).ToArray()), Json);
    internal static IrrigationSnapshot Read(string snapshot) => JsonSerializer.Deserialize<IrrigationSnapshot>(snapshot, Json)
        ?? throw new InvalidOperationException("Snapshot de irrigação ausente.");
}
