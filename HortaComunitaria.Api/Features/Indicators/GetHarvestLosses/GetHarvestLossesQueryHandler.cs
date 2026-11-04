using HortaComunitaria.Api.Domain.Indicators;
using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetHarvestLosses;

internal sealed record GetHarvestLossesRequest(string? From, string? To, int Page, int PageSize, Guid? CycleId, Guid? SpeciesId);

internal sealed record LossOrigin(Guid Id, Guid CycleId, DateOnly HarvestedOn, decimal FreshMassKg, decimal? UsableMassKg, decimal? LossMassKg);
internal sealed record LossAggregate(int Count, int KnownCount, decimal FreshEligible, decimal Loss);
internal sealed record GetHarvestLossesResponse(DateOnly From, DateOnly To, string Unit, decimal? KnownLossKg, decimal? Percentage, int KnownCount, int WithoutUsableMassCount, IndicatorAvailability Availability, Page<LossOrigin> Origins);

internal sealed class GetHarvestLossesQueryHandler(AppDbContext db, ILogger<GetHarvestLossesQueryHandler> logger)
{
    public async Task<Result<GetHarvestLossesResponse>> HandleAsync(Guid gardenId, GetHarvestLossesRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetHarvestLosses", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetHarvestLossesResponse> Reject(Error error) => process.Complete(Result<GetHarvestLossesResponse>.Failure(error));
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

        var cycles = db.CultivationCycles.Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && (request.SpeciesId == null || x.SpeciesId == request.SpeciesId)).Select(x => x.Id);
        var query = db.Harvests.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.HarvestedOn >= range.From && x.HarvestedOn <= range.To && (request.CycleId == null || x.CycleId == request.CycleId) && cycles.Contains(x.CycleId));
        var summary = await query.GroupBy(x => 1).Select(g => new LossAggregate(g.Count(), g.Count(x => x.UsableMassKg != null), g.Sum(x => x.UsableMassKg != null ? x.FreshMassKg : 0), g.Sum(x => x.UsableMassKg != null ? x.FreshMassKg - x.UsableMassKg.Value : 0))).SingleOrDefaultAsync(cancellationToken);
        var count = summary?.Count ?? 0; var known = summary?.KnownCount ?? 0;
        var items = await query.OrderByDescending(x => x.HarvestedOn).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new LossOrigin(x.Id, x.CycleId, x.HarvestedOn, x.FreshMassKg, x.UsableMassKg, x.UsableMassKg == null ? null : x.FreshMassKg - x.UsableMassKg)).ToListAsync(cancellationToken);
        var response = new GetHarvestLossesResponse(range.From, range.To, "kg", known == 0 ? null : summary!.Loss, known == 0 ? null : IndicatorMath.Percentage(summary!.Loss, summary.FreshEligible), known, count - known, IndicatorCoverage.Availability(count, known), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetHarvestLossesResponse>.Success(response), items.Count);
    }
}
