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
namespace HortaComunitaria.Api.Features.Activities.CreateInstitution;

internal sealed record CreateInstitutionRequest([property: JsonRequired] string Name, string? Acronym, string? Type);
internal sealed class CreateInstitutionCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CreateInstitutionCommandHandler> logger)
{
    public async Task<Result<InstitutionResponse>> HandleAsync(CreateInstitutionRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.CreateInstitution", new Dictionary<string, object?> { });
        Result<InstitutionResponse> Reject(Error error) => process.Complete(Result<InstitutionResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var candidate = Institution.Create(request.Name, request.Acronym, request.Type);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.Institutions.Add(entity);
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "Institution", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(InstitutionProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateInstitutionConfirmed", "Created", "Operação CreateInstitution confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<InstitutionResponse>.Success(InstitutionProjection.Map(entity)));
    }
}
