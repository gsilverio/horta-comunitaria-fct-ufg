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
namespace HortaComunitaria.Api.Features.Environment.CreateSamplingPoint;

internal sealed record CreateSamplingPointRequest(string? Code, [property: JsonRequired] string Description, string? Location, Guid? BedId, Guid? SectorId);
internal sealed class CreateSamplingPointCommandHandler(AppDbContext db, CurrentAuthor author, TimeProvider clock, ILogger<CreateSamplingPointCommandHandler> logger)
{
    public async Task<Result<SamplingPointResponse>> HandleAsync(Guid gardenId, CreateSamplingPointRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.CreateSamplingPoint", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<SamplingPointResponse> Reject(Error error) => process.Complete(Result<SamplingPointResponse>.Failure(error));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await WriteLocks.CatalogsAsync(db, cancellationToken);
        await WriteLocks.GardenAsync(db, gardenId, cancellationToken);
        process.Step("CheckingContext", "Conferindo contexto e versão do cadastro");
        if (!await db.Gardens.AnyAsync(x => x.Id == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        if (request.BedId is not null && !await db.Beds.AnyAsync(x => x.Id == request.BedId && x.GardenId == gardenId && x.IsActive, cancellationToken) ||
                    request.SectorId is not null && !await db.IrrigationSectors.AnyAsync(x => x.Id == request.SectorId && x.GardenId == gardenId && x.IsActive, cancellationToken)) return Reject(Error.NotFound());
        var candidate = SamplingPoint.Create(gardenId, request.Code, request.Description, request.Location, request.BedId, request.SectorId);
        if (!candidate.IsSuccess) return Reject(candidate.Error);
        var entity = candidate.Value;
        string? before = null;
        string? reasonValue = null;
        var changed = true;
        db.SamplingPoints.Add(entity);
        if (changed)
        {
            if (await db.SamplingPoints.AnyAsync(x => x.GardenId == gardenId && x.Code == entity.Code && x.Id != entity.Id, cancellationToken)) return Reject(new("catalog.code_conflict", "Código já utilizado.", ErrorCategory.Conflict));
            db.AuditEntries.Add(AuditSnapshots.Create(gardenId, "SamplingPoint", entity.Id, await author.ResolveAsync(cancellationToken), InstantPrecision.ForPersistence(clock.GetUtcNow()),
                before, AuditSnapshots.Capture(SamplingPointProjection.Map(entity)), reasonValue, AuditOperation.Created));
            process.Step("Persisting", "Confirmando cadastro e histórico na mesma transação");
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        process.Event("CreateSamplingPointConfirmed", "Created", "Operação CreateSamplingPoint confirmada para o recurso {ResourceId} na versão {Version}", entity.Id, entity.Version);
        return process.Complete(Result<SamplingPointResponse>.Success(SamplingPointProjection.Map(entity)));
    }
}
