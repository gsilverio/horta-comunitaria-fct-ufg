using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Cultivation.Shared;
namespace HortaComunitaria.Api.Features.Cultivation.ListHarvests;

internal sealed record ListHarvestsRequest(string? From, string? To, int Page, int PageSize, string? Validity, Guid? CycleId, Guid? SpeciesId);
internal sealed class ListHarvestsQueryHandler(AppDbContext db, ILogger<ListHarvestsQueryHandler> logger)
{
    public async Task<Result<Page<HarvestResponse>>> HandleAsync(Guid gardenId, ListHarvestsRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListHarvests", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<HarvestResponse>> Reject(Error error) => process.Complete(Result<Page<HarvestResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error with { Code = "record.invalid_period" });
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        if (request.CycleId == Guid.Empty) return Reject(Error.Validation("cycleId", "Identificador inválido."));
        if (request.SpeciesId == Guid.Empty) return Reject(Error.Validation("speciesId", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.Harvests.AsNoTracking().Where(x => x.GardenId == gardenId && x.HarvestedOn >= period.Value.From && x.HarvestedOn <= period.Value.To && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (request.CycleId == null || x.CycleId == request.CycleId) && (request.SpeciesId == null || db.CultivationCycles.Any(c => c.Id == x.CycleId && c.SpeciesId == request.SpeciesId)));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.HarvestedOn).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(HarvestProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<HarvestResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
