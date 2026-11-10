using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Evidence.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Evidence.CorrectEvidence;

internal sealed record CorrectEvidenceRequest([property: JsonRequired] string Title, string? Description, DateOnly? CapturedOn, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);

internal sealed class CorrectEvidenceCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CorrectEvidenceCommandHandler> logger)
{
    public async Task<Result<EvidenceResponse>> HandleAsync(Guid gardenId, Guid id, CorrectEvidenceRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Evidence.CorrectEvidence", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
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
        var changed = entity.Correct(request.ExpectedVersion, request.Title, request.Description, request.CapturedOn, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        if (changed.Value)
        {
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "EvidenceFile", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(EvidenceProjection.Map(entity)), reason.Value.Value, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando correção e auditoria na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<EvidenceResponse>.Success(EvidenceProjection.Map(entity)));
    }
}
