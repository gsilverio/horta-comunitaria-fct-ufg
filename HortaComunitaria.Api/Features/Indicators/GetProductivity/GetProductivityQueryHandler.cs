using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetProductivity;

internal sealed record GetProductivityRequest(string? From, string? To, int Page, int PageSize, Guid? CycleId, Guid? SpeciesId);

internal sealed record CycleProductivity(Guid CycleId, Guid SpeciesId, decimal OccupiedAreaM2, decimal FreshMassKg, int HarvestCount, decimal ProductivityKgPerM2);
internal sealed record GetProductivityResponse(DateOnly From, DateOnly To, string Definition, decimal? FreshMassKg, int HarvestCount, IndicatorAvailability Availability, Page<CycleProductivity> Origins);

internal sealed class GetProductivityQueryHandler(AppDbContext db, ILogger<GetProductivityQueryHandler> logger)
{
    public async Task<Result<GetProductivityResponse>> HandleAsync(Guid gardenId, GetProductivityRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetProductivity", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetProductivityResponse> Reject(Error error) => process.Complete(Result<GetProductivityResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.CycleId == Guid.Empty || request.SpeciesId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        var cycles = db.CultivationCycles.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && (request.SpeciesId == null || x.SpeciesId == request.SpeciesId) && (request.CycleId == null || x.Id == request.CycleId));
        var harvests = db.Harvests.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.HarvestedOn >= range.From && x.HarvestedOn <= range.To && cycles.Select(c => c.Id).Contains(x.CycleId));
        var harvestCount = await harvests.CountAsync(cancellationToken);
        var total = await harvests.SumAsync(x => (decimal?)x.FreshMassKg, cancellationToken);
        var cycleQuery = cycles.Where(x => harvests.Any(h => h.CycleId == x.Id));
        var count = await cycleQuery.CountAsync(cancellationToken);
        var items = await cycleQuery.OrderBy(x => x.SpeciesId).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new CycleProductivity(x.Id, x.SpeciesId, x.OccupiedAreaM2, harvests.Where(h => h.CycleId == x.Id).Sum(h => h.FreshMassKg), harvests.Count(h => h.CycleId == x.Id), harvests.Where(h => h.CycleId == x.Id).Sum(h => h.FreshMassKg) / x.OccupiedAreaM2)).ToListAsync(cancellationToken);
        var response = new GetProductivityResponse(range.From, range.To, "Massa fresca das colheitas do recorte dividida pela área ocupada de cada ciclo, utilizada uma vez. Sem produtividade global por área física reutilizada.", harvestCount == 0 ? null : total, harvestCount, IndicatorCoverage.Availability(harvestCount, harvestCount), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetProductivityResponse>.Success(response), items.Count);
    }
}
