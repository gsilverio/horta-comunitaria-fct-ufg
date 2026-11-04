using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.GetHarvest;

internal sealed class GetHarvestQueryHandler(AppDbContext db, ILogger<GetHarvestQueryHandler> logger)
{
    public async Task<Result<HarvestResponse>> HandleAsync(Guid gardenId, Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetHarvest", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<HarvestResponse> Reject(Error error) => process.Complete(Result<HarvestResponse>.Failure(error));
        if (gardenId == Guid.Empty || id == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        process.Step("Querying", "Consultando detalhe e metadados do fato");
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        var entity = await db.Harvests.AsNoTracking().Where(x => x.GardenId == gardenId && x.Id == id).Select(HarvestProjection.Select).SingleOrDefaultAsync(cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var allocated = await db.DryingItems.Where(x => x.GardenId == gardenId && x.HarvestId == id && db.DryingLots.Any(l => l.Id == x.DryingLotId && l.Validity == RecordValidity.Valid)).SumAsync(x => (decimal?)x.AllocatedMassKg, cancellationToken) ?? 0m;
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<HarvestResponse>.Success(entity with { AllocatedMassKg = allocated, RemainingUsableMassKg = entity.UsableMassKg is { } usable ? usable - allocated : null }));
    }
}
