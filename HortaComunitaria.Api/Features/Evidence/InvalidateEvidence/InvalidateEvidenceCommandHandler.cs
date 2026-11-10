using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Evidence.InvalidateEvidence;

internal sealed record InvalidateEvidenceRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);

internal sealed class InvalidateEvidenceCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<InvalidateEvidenceCommandHandler> logger)
{
    public async Task<Result<EvidenceResponse>> HandleAsync(Guid gardenId, Guid id, InvalidateEvidenceRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Evidence.InvalidateEvidence", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<EvidenceResponse> Reject(Error error) => process.Complete(Result<EvidenceResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("Querying", "Conferindo fotografia e versão sob proteção transacional");
        var entity = await db.EvidenceFiles.SingleOrDefaultAsync(x => x.GardenId == gardenId && x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var before = AuditSnapshots.Capture(EvidenceProjection.Map(entity));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Invalidate(request.ExpectedVersion, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "EvidenceFile", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(EvidenceProjection.Map(entity)), reason.Value.Value, AuditOperation.Invalidated));
        process.Step("Persisting", "Confirmando invalidação e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("InvalidateEvidenceConfirmed", "Invalidated", "Fotografia {ResourceId} retirada da galeria na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<EvidenceResponse>.Success(EvidenceProjection.Map(entity)));
    }
}
