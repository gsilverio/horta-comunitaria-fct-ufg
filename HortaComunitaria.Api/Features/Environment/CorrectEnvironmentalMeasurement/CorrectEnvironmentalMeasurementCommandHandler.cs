using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.CorrectEnvironmentalMeasurement;

internal sealed record CorrectEnvironmentalMeasurementRequest([property: JsonRequired] Guid SamplingPointId, [property: JsonRequired] Guid VariableId, [property: JsonRequired] DateTimeOffset CollectedAt, [property: JsonRequired] decimal Value, [property: JsonRequired] string Unit, [property: JsonRequired] string Method, [property: JsonRequired] MeasurementQuality Quality, [property: JsonRequired] Guid? ResponsibleParticipantId, [property: JsonRequired] string? Notes, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class CorrectEnvironmentalMeasurementCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CorrectEnvironmentalMeasurementCommandHandler> logger)
{
    public async Task<Result<EnvironmentalMeasurementResponse>> HandleAsync(Guid gardenId, Guid id, CorrectEnvironmentalMeasurementRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.CorrectEnvironmentalMeasurement", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<EnvironmentalMeasurementResponse> Reject(Error error) => process.Complete(Result<EnvironmentalMeasurementResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var proposed = EnvironmentalMeasurementValues.Create(request.SamplingPointId, request.VariableId, InstantPrecision.ForPersistence(request.CollectedAt), request.Value, request.Unit, request.Method, request.Quality, request.ResponsibleParticipantId, request.Notes);
        if (!proposed.IsSuccess) return Reject(proposed.Error);
        var values = proposed.Value;
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingOperation", "Conferindo operação e contexto sob proteção transacional");
        var entity = await db.EnvironmentalMeasurements.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var eligible = entity.ValidateChange(request.ExpectedVersion);
        if (!eligible.IsSuccess) return Reject(eligible.Error);
        var before = AuditSnapshots.Capture(EnvironmentalMeasurementProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        process.Step("ResolvingContext", "Conferindo referências, período e invariantes do fato");
        var context = await EnvironmentRecordContext.ResolveEnvironmentalMeasurementAsync(db, gardenId, values, id, cancellationToken);
        if (!context.IsSuccess) return Reject(context.Error);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Correct(request.ExpectedVersion, reason.Value, values, now, context.Value.BedId, context.Value.SectorId, context.Value.BedSectorAssignmentId);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (!changed.Value)
        {
            await transaction.CommitAsync(cancellationToken);
            return process.Complete(Result<EnvironmentalMeasurementResponse>.Success(EnvironmentalMeasurementProjection.Map(entity)));
        }
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "EnvironmentalMeasurement", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(EnvironmentalMeasurementProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("CorrectEnvironmentalMeasurementConfirmed", "Updated", "Operação CorrectEnvironmentalMeasurement confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<EnvironmentalMeasurementResponse>.Success(EnvironmentalMeasurementProjection.Map(entity)));
    }
}
