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

namespace HortaComunitaria.Api.Features.Gardens.CreateIrrigationSector;

internal sealed record CreateIrrigationSectorRequest(string? Code, [property: JsonRequired] string Name, string? Description);
internal sealed class CreateIrrigationSectorCommandHandler(AppDbContext db, DemoAuthorContext author, TimeProvider clock, ILogger<CreateIrrigationSectorCommandHandler> logger)
{
    public async Task<Result<IrrigationSectorResponse>> HandleAsync(Guid gardenId, CreateIrrigationSectorRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.CreateIrrigationSector", new Dictionary<string, object?> { ["GardenId"] = gardenId, });
        Result<IrrigationSectorResponse> Reject(Error error) => process.Complete(Result<IrrigationSectorResponse>.Failure(error));
        var candidate = IrrigationSector.Create(gardenId, request.Code, request.Name, description: request.Description);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        process.Step("CheckingContext", "Conferindo contexto e chave do cadastro");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if (await db.IrrigationSectors.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict));
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        db.IrrigationSectors.Add(entity);
        db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "IrrigationSector", entity.Id, await author.ResolveAsync(cancellationToken), now, null, AuditSnapshots.Capture(IrrigationSectorProjection.Map(entity))));
        process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Reject(new("catalog.code_conflict", "Código já utilizado neste cadastro.", ErrorCategory.Conflict)); }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateIrrigationSectorConfirmed", "Created", "Operação CreateIrrigationSector confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<IrrigationSectorResponse>.Success(IrrigationSectorProjection.Map(entity)));
    }
}
