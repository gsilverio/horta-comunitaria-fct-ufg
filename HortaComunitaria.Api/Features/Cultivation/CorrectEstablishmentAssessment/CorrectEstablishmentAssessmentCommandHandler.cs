using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.CorrectEstablishmentAssessment;

internal sealed record CorrectEstablishmentAssessmentRequest([property: JsonRequired] Guid PlantingId, [property: JsonRequired] DateOnly AssessedOn, [property: JsonRequired] int SurvivingQuantity, [property: JsonRequired] string Method, [property: JsonRequired] string? Notes, [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectEstablishmentAssessmentCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CorrectEstablishmentAssessmentCommandHandler> logger)
{
    public async Task<Result<EstablishmentAssessmentResponse>> HandleAsync(Guid gardenId, Guid id, CorrectEstablishmentAssessmentRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CorrectEstablishmentAssessment", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<EstablishmentAssessmentResponse> Reject(Error error) => process.Complete(Result<EstablishmentAssessmentResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = EstablishmentAssessmentValues.Create(request.PlantingId, request.AssessedOn, request.SurvivingQuantity, request.Method, request.Notes, request.ResponsibleParticipantId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.EstablishmentAssessments.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(EstablishmentAssessmentProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CultivationRecordContext.ResolveEstablishmentAssessmentAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<EstablishmentAssessmentResponse>.Success(EstablishmentAssessmentProjection.Map(entity)));
        }
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "EstablishmentAssessment", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(EstablishmentAssessmentProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectEstablishmentAssessmentConfirmed", "Updated", "Operação CorrectEstablishmentAssessment confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<EstablishmentAssessmentResponse>.Success(EstablishmentAssessmentProjection.Map(entity)));
    }
}
