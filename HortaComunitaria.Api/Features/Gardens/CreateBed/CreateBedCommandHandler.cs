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

namespace HortaComunitaria.Api.Features.Gardens.CreateBed;

internal sealed record CreateBedRequest(string? Code, [property: JsonRequired] string Name, string? Location);
internal sealed class CreateBedCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CreateBedCommandHandler> logger)
{
    public async Task<Result<BedResponse>> HandleAsync(Guid gardenId, CreateBedRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.CreateBed", new Dictionary<string, object?> { ["GardenId"] = gardenId, });
        Result<BedResponse> Reject(Error error) => process.Complete(Result<BedResponse>.Failure(error));
        var candidate = Bed.Create(gardenId, request.Code, request.Name, location: request.Location);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        process.Step("CheckingContext", "Conferindo contexto e chave do cadastro");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if (await db.Beds.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        db.Beds.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "Bed", entity.Id, await author.ResolveAsync(cancellationToken), now, null, AuditSnapshots.Capture(BedProjection.Map(entity))));
        process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateBedConfirmed", "Created", "Operação CreateBed confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<BedResponse>.Success(BedProjection.Map(entity)));
    }
}
