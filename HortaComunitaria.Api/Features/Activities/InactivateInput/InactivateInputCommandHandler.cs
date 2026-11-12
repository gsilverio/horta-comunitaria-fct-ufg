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
namespace HortaComunitaria.Api.Features.Activities.InactivateInput;

internal sealed record InactivateInputRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InactivateInputCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InactivateInputCommandHandler> logger)
{
    public async Task<Result<AgriculturalInputResponse>> HandleAsync(Guid id, InactivateInputRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.InactivateInput", new Dictionary<string, object?> { });
        Result<AgriculturalInputResponse> Reject(Error error) => process.Complete(Result<AgriculturalInputResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.Inputs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());

        var before = AuditSnapshots.Capture(AgriculturalInputProjection.Map(entity));
        var applied = entity.Inactivate(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()));
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = true;
        var reasonValue = reason.Value.Value;
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "AgriculturalInput", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(AgriculturalInputProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("InactivateInputConfirmed", "Invalidated", "Operação InactivateInput confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<AgriculturalInputResponse>.Success(AgriculturalInputProjection.Map(entity)));
    }
}
