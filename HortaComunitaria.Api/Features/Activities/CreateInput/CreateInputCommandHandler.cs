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
namespace HortaComunitaria.Api.Features.Activities.CreateInput;

internal sealed record CreateInputRequest([property: JsonRequired] string Name, string? Category, [property: JsonRequired] string ReferenceUnit);
internal sealed class CreateInputCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CreateInputCommandHandler> logger)
{
    public async Task<Result<AgriculturalInputResponse>> HandleAsync(CreateInputRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Activities.CreateInput", new Dictionary<string, object?> { });
        Result<AgriculturalInputResponse> Reject(Error error) => process.Complete(Result<AgriculturalInputResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var candidate = AgriculturalInput.Create(request.Name, request.Category, request.ReferenceUnit);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.Inputs.Add(entity);
        if (changed)
        {

            db.AuditEntries.Add(AuditSnapshots.Create(null, "AgriculturalInput", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(AgriculturalInputProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateInputConfirmed", "Created", "Operação CreateInput confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<AgriculturalInputResponse>.Success(AgriculturalInputProjection.Map(entity)));
    }
}
