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
namespace HortaComunitaria.Api.Features.Environment.InactivateEnvironmentalVariable;

internal sealed record InactivateEnvironmentalVariableRequest([property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class InactivateEnvironmentalVariableCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<InactivateEnvironmentalVariableCommandHandler> logger)
{
    public async Task<Result<EnvironmentalVariableResponse>> HandleAsync(Guid id, InactivateEnvironmentalVariableRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.InactivateEnvironmentalVariable", new Dictionary<string, object?> { });
        Result<EnvironmentalVariableResponse> Reject(Error error) => process.Complete(Result<EnvironmentalVariableResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.EnvironmentalVariables.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());

        var before = AuditSnapshots.Capture(EnvironmentalVariableProjection.Map(entity));
        var applied = entity.Inactivate(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()));
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = true;
        var reasonValue = reason.Value.Value;
        if (changed)
        {
            if (await db.EnvironmentalVariables.AnyAsync(x => x.Code == entity.Code && x.Id != entity.Id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(null, "EnvironmentalVariable", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(EnvironmentalVariableProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("InactivateEnvironmentalVariableConfirmed", "Invalidated", "Operação InactivateEnvironmentalVariable confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<EnvironmentalVariableResponse>.Success(EnvironmentalVariableProjection.Map(entity)));
    }
}
