using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HortaComunitaria.Api.Features.Gardens.CreateGarden;

internal sealed record CreateGardenRequest(string? Code, [property: JsonRequired] string Name, string? Location, decimal? InstitutionalAreaM2, decimal? OperationalAreaM2, decimal? ExperimentalAreaM2);
internal sealed class CreateGardenCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CreateGardenCommandHandler> logger)
{
    public async Task<Result<GardenResponse>> HandleAsync(CreateGardenRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.CreateGarden", new Dictionary<string, object?> { });
        Result<GardenResponse> Reject(Error error) => process.Complete(Result<GardenResponse>.Failure(error));
        var candidate = Garden.Create(request.Code, request.Name, location: request.Location, institutionalAreaM2: request.InstitutionalAreaM2, operationalAreaM2: request.OperationalAreaM2, experimentalAreaM2: request.ExperimentalAreaM2);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        process.Step("CheckingContext", "Conferindo contexto e chave do cadastro");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);

        if (await db.Gardens.AnyAsync(x => x.Code == entity.Code, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        db.Gardens.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(entity.Id, "Garden", entity.Id, await author.ResolveAsync(cancellationToken), now, null, AuditSnapshots.Capture(GardenProjection.Map(entity))));
        process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateGardenConfirmed", "Created", "Operação CreateGarden confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<GardenResponse>.Success(GardenProjection.Map(entity)));
    }
}
