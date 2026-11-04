using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Activities.Shared;
namespace HortaComunitaria.Api.Features.Activities.RegisterActivity;

internal sealed record RegisterActivityRequest([property: JsonRequired] Guid OperationId, [property: JsonRequired] string Title, [property: JsonRequired] string Category, [property: JsonRequired] DateOnly PerformedOn, string? Description, Guid? BedId, Guid? SectorId, Guid? CycleId, Guid? OccurrenceId, bool? IsOperational, bool IsSocial, bool IsEducational, string? Objective, string? Modality, int? ReportedAudienceTotal, bool? AudienceListComplete, [property: JsonRequired] ActivityParticipantRequest[]? Participants, [property: JsonRequired] InputUsageRequest[]? InputUsages, [property: JsonRequired] ActivityInstitutionRequest[]? Institutions);
internal sealed class RegisterActivityCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<RegisterActivityCommandHandler> logger)
{
    public async Task<Result<CreatedActivityResponse>> HandleAsync(Guid gardenId, RegisterActivityRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.RegisterActivity", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["OperationId"] = request.OperationId });
        Result<CreatedActivityResponse> Reject(Error error) => process.Complete(Result<CreatedActivityResponse>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = ActivityForm.Create(request.Title, request.Category, request.PerformedOn, request.Description, request.BedId, request.SectorId, request.CycleId, request.OccurrenceId, request.IsOperational ?? true, request.IsSocial, request.IsEducational, request.Objective, request.Modality, request.ReportedAudienceTotal, request.AudienceListComplete, request.Participants, request.InputUsages, request.Institutions);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        if (request.OperationId == Guid.Empty) return Reject(Error.Validation("operationId", "Chave de operação obrigatória."));
        var payload = AuditSnapshots.Capture(values);
        var existing = await db.Activities.AsNoTracking().Include(x => x.Participants).Include(x => x.InputUsages).Include(x => x.Institutions).AsSplitQuery().SingleOrDefaultAsync(x => x.GardenId == gardenId && x.OperationId == request.OperationId, cancellationToken);
        if (existing is not null)
        {
            using var original = System.Text.Json.JsonDocument.Parse(existing.CreationPayload);
            using var candidatePayload = System.Text.Json.JsonDocument.Parse(payload);
            if (!System.Text.Json.JsonElement.DeepEquals(original.RootElement, candidatePayload.RootElement)) return Reject(new("record.operation_conflict", "Chave de operação já usada com outro conteúdo.", ErrorCategory.Conflict));
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<CreatedActivityResponse>.Success(new(false, ActivityProjection.Map(existing))));
        }
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await ActivitiesRecordContext.ResolveActivityAsync(db, gardenId, values, Guid.Empty, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var candidate = Activity.Create(gardenId, request.OperationId, values, await author.ResolveAsync(cancellationToken), now, payload);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        db.Activities.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Activity", entity.Id, entity.RecordedByUserId, now, null, AuditSnapshots.Capture(ActivityProjection.Map(entity))));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("RegisterActivityConfirmed", "Created", "Operação RegisterActivity confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<CreatedActivityResponse>.Success(new(true, ActivityProjection.Map(entity))));
    }
}
