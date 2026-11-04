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
namespace HortaComunitaria.Api.Features.Activities.UpdateInstitution;

internal sealed record UpdateInstitutionRequest([property: JsonRequired] string Name, [property: JsonRequired] string? Acronym, [property: JsonRequired] string? Type, [property: JsonRequired] int ExpectedVersion, [property: JsonRequired] string Reason);
internal sealed class UpdateInstitutionCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<UpdateInstitutionCommandHandler> logger)
{
    public async Task<Result<InstitutionResponse>> HandleAsync(Guid id, UpdateInstitutionRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.UpdateInstitution", new Dictionary<string, object?> { });
        Result<InstitutionResponse> Reject(Error error) => process.Complete(Result<InstitutionResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var reason = ChangeReason.Create(request.Reason);
        if (!reason.IsSuccess) return Reject(reason.Error);
        var entity = await db.Institutions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return Reject(Error.NotFound());

        var before = AuditSnapshots.Capture(InstitutionProjection.Map(entity));
        var applied = entity.Update(request.ExpectedVersion, reason.Value, InstantPrecision.ForPersistence(clock.GetUtcNow()), request.Name, request.Acronym, request.Type);
        if (!applied.IsSuccess) return Reject(applied.Error);
        var changed = applied.Value;
        var reasonValue = reason.Value.Value;
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "Institution", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(InstitutionProjection.Map(entity)), reasonValue, AuditOperation.Corrected));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("UpdateInstitutionConfirmed", "Updated", "Operação UpdateInstitution confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<InstitutionResponse>.Success(InstitutionProjection.Map(entity)));
    }
}
