using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Evidence;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Evidence.Shared;

/// <summary>Metadados públicos da fotografia; o conteúdo é obtido pela rota de conteúdo.</summary>
internal sealed record EvidenceResponse(Guid Id, Guid GardenId, Guid OperationId, EvidenceTarget TargetType, Guid TargetId, string Title, string? Description,
    DateOnly? CapturedOn, string OriginalFileName, string ContentType, long SizeBytes, Guid RecordedByUserId, DateTimeOffset RecordedAt,
    DateTimeOffset? UpdatedAt, int Version, RecordValidity Validity);

internal sealed record CreatedEvidenceResponse(bool Created, EvidenceResponse Detail);

internal static class EvidenceProjection
{
    internal static EvidenceResponse Map(EvidenceFile x) => new(x.Id, x.GardenId, x.OperationId, x.Target, x.TargetId, x.Title, x.Description,
        x.CapturedOn, x.OriginalFileName, x.ContentType, x.SizeBytes, x.RecordedByUserId, x.RecordedAt, x.UpdatedAt, x.Version, x.Validity);
}

internal static class EvidenceTargets
{
    /// <summary>Interpreta o tipo do vínculo pelo nome documentado, rejeitando números.</summary>
    internal static Result<EvidenceTarget> Parse(string? value, string field = "targetType") =>
        value is not null && Enum.GetNames<EvidenceTarget>().Contains(value, StringComparer.Ordinal) && Enum.TryParse<EvidenceTarget>(value, out var target)
            ? Result<EvidenceTarget>.Success(target)
            : Result<EvidenceTarget>.Failure(Error.Validation(field, "Tipo de registro não suportado para fotografias."));

    /// <summary>Confere que o registro existe na mesma horta e ainda aceita novas fotografias.</summary>
    internal static async Task<Result<Unit>> EnsureAttachableAsync(AppDbContext db, Guid gardenId, EvidenceTarget target, Guid targetId, CancellationToken token)
    {
        if (targetId == Guid.Empty) return Result<Unit>.Failure(Error.Validation("targetId", "Identificador inválido."));
        var state = target switch
        {
            EvidenceTarget.Bed => await db.Beds.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)x.IsActive).SingleOrDefaultAsync(token),
            EvidenceTarget.CultivationCycle => await db.CultivationCycles.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.Planting => await db.Plantings.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.EstablishmentAssessment => await db.EstablishmentAssessments.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.AgronomicObservation => await db.AgronomicObservations.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.Harvest => await db.Harvests.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.DryingLot => await db.DryingLots.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.Activity => await db.Activities.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            EvidenceTarget.Occurrence => await db.Occurrences.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == targetId).Select(x => (bool?)(x.Validity == RecordValidity.Valid)).SingleOrDefaultAsync(token),
            _ => null
        };
        if (state is null) return Result<Unit>.Failure(new("evidence.target_not_found", "Registro vinculado não encontrado nesta horta.", ErrorCategory.NotFound));
        if (state is false) return Result<Unit>.Failure(new("evidence.target_inactive", "O registro está invalidado ou inativo e não aceita novas fotografias.", ErrorCategory.Conflict));
        return Result<Unit>.Success(new());
    }

    /// <summary>Filtro da consulta pelo destino concreto.</summary>
    internal static IQueryable<EvidenceFile> WhereTarget(IQueryable<EvidenceFile> query, EvidenceTarget target, Guid targetId) => target switch
    {
        EvidenceTarget.Bed => query.Where(x => x.BedId == targetId),
        EvidenceTarget.CultivationCycle => query.Where(x => x.CycleId == targetId),
        EvidenceTarget.Planting => query.Where(x => x.PlantingId == targetId),
        EvidenceTarget.EstablishmentAssessment => query.Where(x => x.EstablishmentAssessmentId == targetId),
        EvidenceTarget.AgronomicObservation => query.Where(x => x.AgronomicObservationId == targetId),
        EvidenceTarget.Harvest => query.Where(x => x.HarvestId == targetId),
        EvidenceTarget.DryingLot => query.Where(x => x.DryingLotId == targetId),
        EvidenceTarget.Activity => query.Where(x => x.ActivityId == targetId),
        _ => query.Where(x => x.OccurrenceId == targetId)
    };

    /// <summary>Filtro somente pelo tipo, para a galeria geral.</summary>
    internal static IQueryable<EvidenceFile> WhereType(IQueryable<EvidenceFile> query, EvidenceTarget target) => target switch
    {
        EvidenceTarget.Bed => query.Where(x => x.BedId != null),
        EvidenceTarget.CultivationCycle => query.Where(x => x.CycleId != null),
        EvidenceTarget.Planting => query.Where(x => x.PlantingId != null),
        EvidenceTarget.EstablishmentAssessment => query.Where(x => x.EstablishmentAssessmentId != null),
        EvidenceTarget.AgronomicObservation => query.Where(x => x.AgronomicObservationId != null),
        EvidenceTarget.Harvest => query.Where(x => x.HarvestId != null),
        EvidenceTarget.DryingLot => query.Where(x => x.DryingLotId != null),
        EvidenceTarget.Activity => query.Where(x => x.ActivityId != null),
        _ => query.Where(x => x.OccurrenceId != null)
    };
}
