using System.Data;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Irrigation.Shared;

/// <summary>Autoria demonstrativa, sem credenciais ou identificação autenticada de pessoa.</summary>
internal sealed record RecordedByResponse(Guid Id, string DisplayName, bool IsDemonstration);
/// <summary>Canteiro e vínculo histórico efetivamente utilizados pelo evento.</summary>
internal sealed record IrrigatedBedResponse(Guid Id, string Code, string Name, Guid AssignmentId);
/// <summary>Representação pública do fato; não expõe payload de repetição nem chave de operação.</summary>
internal sealed record IrrigationDetailResponse(Guid Id, Guid SectorId, string SectorName,
    DateTimeOffset StartedAt, DateTimeOffset? EndedAt, decimal? ActiveDurationMinutes, decimal? VolumeLiters,
    decimal? FlowRateLitersPerMinute, VolumeSource? VolumeSource, string? VolumeMethod, IrrigationStatus Status,
    Guid? ResponsibleParticipantId, string? Notes, RecordValidity Validity, int Version,
    DateTimeOffset RecordedAt, DateTimeOffset? UpdatedAt, RecordedByResponse RecordedBy, IReadOnlyList<IrrigatedBedResponse> Beds)
{
    public IReadOnlyList<Guid> BedIds => Beds.Select(x => x.Id).ToArray();
}

internal static class IrrigationDetailProjection
{
    internal static async Task<IrrigationDetailResponse?> LoadAsync(AppDbContext db, Guid gardenId, Guid id,
        CancellationToken cancellationToken)
    {
        // Detalhe e filhos precisam do mesmo estado agora que o fato pode ser corrigido.
        if (db.Database.CurrentTransaction is null)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
            var snapshot = await LoadAsync(db, gardenId, id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return snapshot;
        }
        var detail = await (from irrigation in db.Irrigations.AsNoTracking()
                            join sector in db.IrrigationSectors.AsNoTracking() on irrigation.IrrigationSectorId equals sector.Id
                            join author in db.Users.AsNoTracking() on irrigation.RecordedByUserId equals author.Id
                            where irrigation.GardenId == gardenId && irrigation.Id == id
                            select new IrrigationDetailResponse(irrigation.Id, sector.Id, sector.Name, irrigation.StartedAt,
                                irrigation.EndedAt, irrigation.ActiveDurationMinutes, irrigation.VolumeLiters,
                                irrigation.FlowRateLitersPerMinute, irrigation.VolumeSource, irrigation.VolumeMethod,
                                irrigation.Status, irrigation.ResponsibleParticipantId, irrigation.Notes,
                                irrigation.Validity, irrigation.Version, irrigation.RecordedAt, irrigation.UpdatedAt,
                                new RecordedByResponse(author.Id, author.DisplayName, author.AuthenticationIdentifier == "demo:operator"),
                                Array.Empty<IrrigatedBedResponse>()))
                            .TagWith("Irrigation.Detail").SingleOrDefaultAsync(cancellationToken);
        if (detail is null) return null;
        var beds = await (from link in db.IrrigationBeds.AsNoTracking()
                          join bed in db.Beds.AsNoTracking() on link.BedId equals bed.Id
                          where link.GardenId == gardenId && link.IrrigationId == id
                          orderby bed.Code, bed.Id
                          select new IrrigatedBedResponse(bed.Id, bed.Code, bed.Name, link.BedSectorAssignmentId))
                          .ToListAsync(cancellationToken);
        return detail with { Beds = beds };
    }
}
