using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetBasicCosts;

internal sealed record GetBasicCostsRequest(string? From, string? To, int Page, int PageSize, string? Category, Guid? BedId, Guid? CycleId);

internal sealed record CostOrigin(Guid Id, DateOnly EntryDate, string Category, FinancialNature Nature, decimal AmountBrl, Guid? BedId, Guid? CycleId);
internal sealed record CostSummary(int Count, int PlannedCount, int ActualCount, decimal Planned, decimal Actual);
internal sealed record GetBasicCostsResponse(DateOnly From, DateOnly To, string Currency, decimal? PlannedBrl, decimal? ActualBrl, decimal? DifferenceBrl, int PlannedCount, int ActualCount, string Comparison, IndicatorAvailability Availability, Page<CostOrigin> Origins);

internal sealed class GetBasicCostsQueryHandler(AppDbContext db, ILogger<GetBasicCostsQueryHandler> logger)
{
    public async Task<Result<GetBasicCostsResponse>> HandleAsync(Guid gardenId, GetBasicCostsRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetBasicCosts", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetBasicCostsResponse> Reject(Error error) => process.Complete(Result<GetBasicCostsResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.BedId == Guid.Empty || request.CycleId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        if (request.BedId != null && request.CycleId != null) return Reject(Error.Validation("context", "Escolha canteiro ou ciclo."));
        var query = db.FinancialEntries.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.EntryDate >= range.From && x.EntryDate <= range.To && (request.Category == null || x.Category == request.Category.Trim()) && (request.BedId == null || x.BedId == request.BedId) && (request.CycleId == null || x.CycleId == request.CycleId));
        var summary = await query.GroupBy(x => 1).Select(g => new CostSummary(g.Count(), g.Count(x => x.Nature == FinancialNature.Planned), g.Count(x => x.Nature == FinancialNature.Actual), g.Sum(x => x.Nature == FinancialNature.Planned ? x.AmountBrl : 0), g.Sum(x => x.Nature == FinancialNature.Actual ? x.AmountBrl : 0))).SingleOrDefaultAsync(cancellationToken);
        var count = summary?.Count ?? 0;
        var items = await query.OrderByDescending(x => x.EntryDate).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new CostOrigin(x.Id, x.EntryDate, x.Category, x.Nature, x.AmountBrl, x.BedId, x.CycleId)).ToListAsync(cancellationToken);
        var response = new GetBasicCostsResponse(range.From, range.To, "BRL", summary?.PlannedCount > 0 ? summary.Planned : null, summary?.ActualCount > 0 ? summary.Actual : null, summary == null ? null : summary.Actual - summary.Planned, summary?.PlannedCount ?? 0, summary?.ActualCount ?? 0, summary == null ? "Sem lançamentos" : summary.PlannedCount == 0 ? "Comparação do recorte registrado sem registros de previsão" : summary.ActualCount == 0 ? "Comparação do recorte registrado sem registros de realização" : "Realizado menos previsto do recorte registrado", IndicatorCoverage.Availability(count, count), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetBasicCostsResponse>.Success(response), items.Count);
    }
}
