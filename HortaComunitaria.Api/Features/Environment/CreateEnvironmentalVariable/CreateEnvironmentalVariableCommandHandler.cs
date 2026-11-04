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
namespace HortaComunitaria.Api.Features.Environment.CreateEnvironmentalVariable;

internal sealed record CreateEnvironmentalVariableRequest(string? Code, [property: JsonRequired] string Name, [property: JsonRequired] string Quantity, [property: JsonRequired] string ReferenceUnit, string? Description, bool? IsSoilMoisture);
internal sealed class CreateEnvironmentalVariableCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CreateEnvironmentalVariableCommandHandler> logger)
{
    public async Task<Result<EnvironmentalVariableResponse>> HandleAsync(CreateEnvironmentalVariableRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.CreateEnvironmentalVariable", new Dictionary<string, object?> { });
        Result<EnvironmentalVariableResponse> Reject(Error error) => process.Complete(Result<EnvironmentalVariableResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");

        var candidate = EnvironmentalVariable.Create(request.Code, request.Name, request.Quantity, request.ReferenceUnit, request.Description, request.IsSoilMoisture);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.EnvironmentalVariables.Add(entity);
        if (changed)
        {
            if (await db.EnvironmentalVariables.AnyAsync(x => x.Code == entity.Code && x.Id != entity.Id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(null, "EnvironmentalVariable", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(EnvironmentalVariableProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateEnvironmentalVariableConfirmed", "Created", "Operação CreateEnvironmentalVariable confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<EnvironmentalVariableResponse>.Success(EnvironmentalVariableProjection.Map(entity)));
    }
}
