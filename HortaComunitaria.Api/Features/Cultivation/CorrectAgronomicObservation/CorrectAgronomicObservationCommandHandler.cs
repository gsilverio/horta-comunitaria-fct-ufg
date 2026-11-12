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
namespace HortaComunitaria.Api.Features.Cultivation.CorrectAgronomicObservation;

internal sealed record CorrectAgronomicObservationRequest([property: JsonRequired] Guid CycleId, [property: JsonRequired] DateOnly ObservedOn, [property: JsonRequired] string Category, [property: JsonRequired] string Description, [property: JsonRequired] decimal? Measurement, [property: JsonRequired] string? Unit, [property: JsonRequired] string? Method, [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectAgronomicObservationCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CorrectAgronomicObservationCommandHandler> logger)
{
    public async Task<Result<AgronomicObservationResponse>> HandleAsync(Guid gardenId, Guid id, CorrectAgronomicObservationRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.CorrectAgronomicObservation", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<AgronomicObservationResponse> Reject(Error error) => process.Complete(Result<AgronomicObservationResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = AgronomicObservationValues.Create(request.CycleId, request.ObservedOn, request.Category, request.Description, request.Measurement, request.Unit, request.Method, request.ResponsibleParticipantId);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.AgronomicObservations.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(AgronomicObservationProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await CultivationRecordContext.ResolveAgronomicObservationAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<AgronomicObservationResponse>.Success(AgronomicObservationProjection.Map(entity)));
        }
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "AgronomicObservation", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(AgronomicObservationProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectAgronomicObservationConfirmed", "Updated", "Operação CorrectAgronomicObservation confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<AgronomicObservationResponse>.Success(AgronomicObservationProjection.Map(entity)));
    }
}
