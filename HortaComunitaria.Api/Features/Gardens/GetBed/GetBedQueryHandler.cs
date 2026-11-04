using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Gardens.GetBed;

internal sealed class GetBedQueryHandler(AppDbContext db, TimeProvider clock, ILogger<GetBedQueryHandler> logger)
{
    public async Task<Result<BedResponse>> HandleAsync(Guid gardenId, Guid id, string? on, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Gardens.GetBed", new Dictionary<string, object?> { ["GardenId"] = gardenId, ["ResourceId"] = id });
        Result<BedResponse> Reject(Error error) => process.Complete(Result<BedResponse>.Failure(error));
        if (id == Guid.Empty || gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var selectedDate = LocalPeriod.LocalDate(clock.GetUtcNow());
        if (on is not null && !DateOnly.TryParseExact(on, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out selectedDate))
            return Reject(Error.Validation("on", "Data deve usar YYYY-MM-DD."));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        process.Step("Querying", "Consultando detalhe do cadastro preservado");
        var entity = await db.Beds.AsNoTracking().Where(x => x.Id == id && x.GardenId == gardenId).Select(BedProjection.Select).SingleOrDefaultAsync(cancellationToken);
        if (entity is null) return Reject(Error.NotFound());
        var configuration = await db.BedConfigurations.AsNoTracking().Where(x => x.GardenId == gardenId && x.BedId == id && x.ValidFrom <= selectedDate && (x.ValidUntil == null || selectedDate < x.ValidUntil))
            .Select(BedConfigurationProjection.Select).SingleOrDefaultAsync(cancellationToken);
        var assignment = await db.BedSectorAssignments.AsNoTracking().Where(x => x.GardenId == gardenId && x.BedId == id && x.ValidFrom <= selectedDate && (x.ValidUntil == null || selectedDate < x.ValidUntil))
            .Select(BedSectorAssignmentProjection.Select).SingleOrDefaultAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<BedResponse>.Success(entity with { On = selectedDate, Configuration = configuration, SectorAssignment = assignment }));
    }
}
