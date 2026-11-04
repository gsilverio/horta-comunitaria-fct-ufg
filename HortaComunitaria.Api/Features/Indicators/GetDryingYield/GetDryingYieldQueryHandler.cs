using HortaComunitaria.Api.Domain.Indicators;
using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetDryingYield;

internal sealed record GetDryingYieldRequest(string? From, string? To, int Page, int PageSize, Guid? LotId, Guid? SpeciesId);

internal sealed record DryingOrigin(Guid Id, string Code, DateOnly StartedOn, DryingStatus Status, decimal AllocatedMassKg, decimal? TotalDryMassKg, bool MixedSpecies, bool Eligible, decimal? Percentage);
internal sealed record DryingSummary(int Count, int EligibleCount, int IncompleteCount, int MixedCount, decimal Allocated, decimal Dry);
internal sealed record GetDryingYieldResponse(DateOnly From, DateOnly To, string Definition, decimal? KnownDryMassKg, decimal? EligibleAllocatedMassKg, decimal? Percentage, int EligibleCount, int IncompleteCount, int MixedSpeciesCount, IndicatorAvailability Availability, Page<DryingOrigin> Origins);

internal sealed class GetDryingYieldQueryHandler(AppDbContext db, ILogger<GetDryingYieldQueryHandler> logger)
{
    public async Task<Result<GetDryingYieldResponse>> HandleAsync(Guid gardenId, GetDryingYieldRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetDryingYield", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetDryingYieldResponse> Reject(Error error) => process.Complete(Result<GetDryingYieldResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.LotId == Guid.Empty || request.SpeciesId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        var speciesHarvests = from h in db.Harvests join c in db.CultivationCycles on new { h.GardenId, Id = h.CycleId } equals new { c.GardenId, c.Id } select new { h.Id, c.SpeciesId };
        var lots = db.DryingLots.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.StartedOn >= range.From && x.StartedOn <= range.To && (request.LotId == null || x.Id == request.LotId) && (request.SpeciesId == null || x.Items.Any(i => speciesHarvests.Any(h => h.Id == i.HarvestId && h.SpeciesId == request.SpeciesId))));
        var projection = lots.Select(x => new { x.Id, x.Code, x.StartedOn, x.Status, x.TotalDryMassKg, Allocated = x.Items.Sum(i => i.AllocatedMassKg), Mixed = speciesHarvests.Where(h => x.Items.Any(i => i.HarvestId == h.Id)).Select(h => h.SpeciesId).Distinct().Count() > 1, Eligible = x.Status == DryingStatus.Completed && x.TotalDryMassKg != null && x.Items.Sum(i => i.AllocatedMassKg) > 0 && (request.SpeciesId == null || !x.Items.Any(i => speciesHarvests.Any(h => h.Id == i.HarvestId && h.SpeciesId != request.SpeciesId))) });
        var summary = await projection.GroupBy(x => 1).Select(g => new DryingSummary(g.Count(), g.Count(x => x.Eligible), g.Count(x => x.Status != DryingStatus.Completed), g.Count(x => x.Mixed), g.Sum(x => x.Eligible ? x.Allocated : 0), g.Sum(x => x.Eligible ? x.TotalDryMassKg ?? 0 : 0))).SingleOrDefaultAsync(cancellationToken);
        var count = summary?.Count ?? 0; var known = summary?.EligibleCount ?? 0;
        var pageRows = await projection.OrderByDescending(x => x.StartedOn).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var items = pageRows.Select(x => new DryingOrigin(x.Id, x.Code, x.StartedOn, x.Status, x.Allocated, x.TotalDryMassKg, x.Mixed, x.Eligible, x.Eligible ? IndicatorMath.Percentage(x.TotalDryMassKg!.Value, x.Allocated) : null)).ToList();
        var response = new GetDryingYieldResponse(range.From, range.To, "Massa seca conhecida / massa destinada dos lotes elegíveis × 100. Lote misto não tem massa seca atribuível por espécie.", known == 0 ? null : summary!.Dry, known == 0 ? null : summary!.Allocated, known == 0 ? null : IndicatorMath.Percentage(summary!.Dry, summary.Allocated), known, summary?.IncompleteCount ?? 0, summary?.MixedCount ?? 0, IndicatorCoverage.Availability(count, known), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetDryingYieldResponse>.Success(response), items.Count);
    }
}
