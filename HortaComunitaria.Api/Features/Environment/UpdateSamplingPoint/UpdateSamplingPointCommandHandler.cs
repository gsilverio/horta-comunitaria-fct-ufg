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
namespace HortaComunitaria.Api.Features.Environment.UpdateSamplingPoint;

internal sealed record UpdateSamplingPointRequest(string? Code, [property: JsonRequired] string Description, [property: JsonRequired] string? Location, [property: JsonRequired] Guid? BedId, [property: JsonRequired] Guid? SectorId, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class UpdateSamplingPointCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<UpdateSamplingPointCommandHandler> logger)
{
    public async Task<Result<SamplingPointResponse>> HandleAsync(Guid gardenId, Guid id, UpdateSamplingPointRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.UpdateSamplingPoint", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<SamplingPointResponse> Reject(Error error) => process.Complete(Result<SamplingPointResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.SamplingPoints.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        if (request.BedId is not null && !await db.Beds.AnyAsync(x => x.Id == request.BedId && x.GardenId == gardenId && x.IsActive, cancellationToken) ||
            request.SectorId is not null && !await db.IrrigationSectors.AnyAsync(x => x.Id == request.SectorId && x.GardenId == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if ((entity.BedId != request.BedId || entity.SectorId != request.SectorId) && await db.EnvironmentalMeasurements.AnyAsync(x => x.SamplingPointId == id, cancellationToken)) return Reject(RecordRules.HasDependents("Ponto já utilizado não pode ser deslocado; cadastre outro ponto."));
        var before = AuditSnapshots.Capture(SamplingPointProjection.Map(entity));
        var applied = entity.Update(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()), request.Code ?? entity.Code, request.Description, request.Location, request.BedId, request.SectorId);
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = applied.Value;
        var reasonValue = reason.Value.Value;
        if (changed)
        {
            if (await db.SamplingPoints.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code && x.Id != entity.Id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "SamplingPoint", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(SamplingPointProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("UpdateSamplingPointConfirmed", "Updated", "Operação UpdateSamplingPoint confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<SamplingPointResponse>.Success(SamplingPointProjection.Map(entity)));
    }
}
