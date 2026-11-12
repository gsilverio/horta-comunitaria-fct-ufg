using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.CorrectActivity;

internal sealed record CorrectActivityRequest([property: JsonRequired] string Title, [property: JsonRequired] string Category, [property: JsonRequired] DateOnly PerformedOn, [property: JsonRequired] string? Description, [property: JsonRequired] Guid? BedId, [property: JsonRequired] Guid? SectorId, [property: JsonRequired] Guid? CycleId, [property: JsonRequired] Guid? OccurrenceId, [property: JsonRequired] bool IsOperational, [property: JsonRequired] bool IsSocial, [property: JsonRequired] bool IsEducational, [property: JsonRequired] string? Objective, [property: JsonRequired] string? Modality, [property: JsonRequired] int? ReportedAudienceTotal, [property: JsonRequired] bool? AudienceListComplete, [property: JsonRequired] ActivityParticipantRequest[]? Participants, [property: JsonRequired] InputUsageRequest[]? InputUsages, [property: JsonRequired] ActivityInstitutionRequest[]? Institutions, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectActivityCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CorrectActivityCommandHandler> logger)
{
    public async Task<Result<ActivityResponse>> HandleAsync(Guid gardenId, Guid id, CorrectActivityRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.CorrectActivity", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<ActivityResponse> Reject(Error error) => process.Complete(Result<ActivityResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = ActivityForm.Create(request.Title, request.Category, request.PerformedOn, request.Description, request.BedId, request.SectorId, request.CycleId, request.OccurrenceId, request.IsOperational, request.IsSocial, request.IsEducational, request.Objective, request.Modality, request.ReportedAudienceTotal, request.AudienceListComplete, request.Participants, request.InputUsages, request.Institutions);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.Activities.Include(x => x.Participants).Include(x => x.InputUsages).Include(x => x.Institutions).AsSplitQuery().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var previousParticipants = entity.Participants.ToArray();
        var previousInputUsages = entity.InputUsages.ToArray();
        var previousInstitutions = entity.Institutions.ToArray();
        var before = AuditSnapshots.Capture(ActivityProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await ActivitiesRecordContext.ResolveActivityAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<ActivityResponse>.Success(ActivityProjection.Map(entity)));
        }
        db.ActivityParticipants.RemoveRange(previousParticipants.Where(x => !entity.Participants.Any(current => current.ParticipantId == x.ParticipantId)));
        db.ActivityParticipants.AddRange(entity.Participants.Where(x => !previousParticipants.Any(previous => previous.ParticipantId == x.ParticipantId)));
        db.InputUsages.RemoveRange(previousInputUsages.Where(x => !entity.InputUsages.Any(current => current.Id == x.Id)));
        db.InputUsages.AddRange(entity.InputUsages.Where(x => !previousInputUsages.Any(previous => previous.Id == x.Id)));
        db.ActivityInstitutions.RemoveRange(previousInstitutions.Where(x => !entity.Institutions.Any(current => current.InstitutionId == x.InstitutionId)));
        db.ActivityInstitutions.AddRange(entity.Institutions.Where(x => !previousInstitutions.Any(previous => previous.InstitutionId == x.InstitutionId)));
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Activity", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(ActivityProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectActivityConfirmed", "Updated", "Operação CorrectActivity confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<ActivityResponse>.Success(ActivityProjection.Map(entity)));
    }
}
