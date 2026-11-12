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
namespace HortaComunitaria.Api.Features.Activities.InvalidateActivity;

internal sealed record InvalidateActivityRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InvalidateActivityCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InvalidateActivityCommandHandler> logger)
{
    public async Task<Result<ActivityResponse>> HandleAsync(Guid gardenId, Guid id, InvalidateActivityRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.InvalidateActivity", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id, ["ExpectedVersion"] = request.ExpectedVersion });
        Result<ActivityResponse> Reject(Error error) => process.Complete(Result<ActivityResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
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
        var before = AuditSnapshots.Capture(ActivityProjection.Map(entity));
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var changed = entity.Invalidate(request.ExpectedVersion, reason.Value, now);
        if (!changed.IsSuccess) return Reject(changed.Error);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Activity", id, await author.ResolveAsync(cancellationToken), now, before, AuditSnapshots.Capture(ActivityProjection.Map(entity)), reason.Value.Value, AuditOperation.Invalidated));
        process.Step("Persisting", "Confirmando fato e auditoria na mesma transação");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        process.Event("InvalidateActivityConfirmed", "Invalidated", "Operação InvalidateActivity confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<ActivityResponse>.Success(ActivityProjection.Map(entity)));
    }
}
