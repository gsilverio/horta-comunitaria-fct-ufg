using System.Data;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetWaterConsumption;

/// <summary>Disponibilidade calculada a partir da cobertura dos volumes coletados.</summary>
internal enum ConsumptionAvailability { NoData, Unavailable, Partial, Available }
internal sealed record GetWaterConsumptionRequest(string? From, string? To, Guid? SectorId, int Page, int PageSize);
/// <summary>Cobertura do consumo conhecido; desconhecido não vira zero.</summary>
internal sealed record WaterCoverage(decimal? KnownVolumeLiters, int EventCount, int EventsWithKnownVolume,
    int EventsWithoutVolume, ConsumptionAvailability Availability, bool IncludesEstimatedVolume);
/// <summary>Execução operacional e cobertura da duração, separadas da cobertura do volume.</summary>
internal sealed record WaterExecutionCoverage(int CompletedCount, int InterruptedCount, decimal? KnownActiveDurationMinutes,
    int EventsWithKnownDuration, int EventsWithoutDuration, ConsumptionAvailability DurationAvailability);
/// <summary>Uma linha por setor, inclusive quando não há coleta.</summary>
internal sealed record SectorConsumptionResponse(Guid SectorId, string Code, string Name,
    decimal? KnownVolumeLiters, int EventCount, int EventsWithKnownVolume, int EventsWithoutVolume,
    ConsumptionAvailability Availability, bool IncludesEstimatedVolume,
    int CompletedCount, int InterruptedCount, decimal? KnownActiveDurationMinutes, int EventsWithKnownDuration, int EventsWithoutDuration, ConsumptionAvailability DurationAvailability);
/// <summary>Resumo do recorte inteiro e página de setores do mesmo snapshot.</summary>
internal sealed record WaterConsumptionResponse(DateOnly From, DateOnly To, string TimeZone, string Unit,
    Guid? SectorId, string Definition, WaterCoverage Summary, Page<SectorConsumptionResponse> BySector)
{
    public WaterExecutionCoverage ExecutionSummary { get; init; } = new(0, 0, null, 0, 0, ConsumptionAvailability.NoData);
}
internal sealed record ConsumptionAggregate(Guid SectorId, int EventCount, int KnownCount, decimal KnownVolume, bool Estimated,
    int CompletedCount, int InterruptedCount, decimal KnownDuration, int DurationCount);

internal sealed class GetWaterConsumptionQueryHandler(AppDbContext db, ILogger<GetWaterConsumptionQueryHandler> logger)
{
    public async Task<Result<WaterConsumptionResponse>> HandleAsync(Guid gardenId, GetWaterConsumptionRequest request,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetWaterConsumption",
            new Dictionary<string, object?> { ["GardenId"] = gardenId, ["SectorId"] = request.SectorId });
        Result<WaterConsumptionResponse> Reject(Error error) => process.Complete(Result<WaterConsumptionResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.SectorId == Guid.Empty)
            return Reject(Error.Validation("id", "Identificadores devem ser não vazios."));
        process.Step("CheckingContext", "Conferindo horta e setor no snapshot do indicador de consumo");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken) ||
            request.SectorId is not null && !await db.IrrigationSectors.AsNoTracking().AnyAsync(
                x => x.Id == request.SectorId && x.GardenId == gardenId, cancellationToken))
            return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Agregando eventos válidos de {From} a {To}, sem multiplicar volumes por canteiro", range.From, range.To);
        var events = db.Irrigations.AsNoTracking().Where(x => x.GardenId == gardenId &&
            x.Validity == RecordValidity.Valid && x.StartedAt >= range.StartUtc && x.StartedAt < range.EndUtc &&
            (request.SectorId == null || x.IrrigationSectorId == request.SectorId));
        var aggregate = await events.TagWith("WaterConsumption.Summary").GroupBy(x => 1)
            .Select(group => new ConsumptionAggregate(Guid.Empty, group.Count(), group.Count(x => x.VolumeLiters != null),
                group.Sum(x => x.VolumeLiters ?? 0m), group.Any(x => x.VolumeLiters != null && x.VolumeSource == VolumeSource.Estimated),
                group.Count(x => x.Status == IrrigationStatus.Completed), group.Count(x => x.Status == IrrigationStatus.Interrupted),
                group.Sum(x => x.ActiveDurationMinutes ?? 0m), group.Count(x => x.ActiveDurationMinutes != null)))
            .SingleOrDefaultAsync(cancellationToken);
        process.Step("LoadingSectorPage", "Consultando página {Page} de setores e cobertura dos volumes conhecidos", pagination.Page);
        var sectors = db.IrrigationSectors.AsNoTracking().Where(x => x.GardenId == gardenId &&
            (request.SectorId == null || x.Id == request.SectorId) &&
            (x.IsActive || events.Any(irrigation => irrigation.IrrigationSectorId == x.Id)));
        var count = await sectors.CountAsync(cancellationToken);
        var sectorPage = await sectors.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(cancellationToken);
        var ids = sectorPage.Select(x => x.Id).ToArray();
        var pageAggregates = await events.Where(x => ids.Contains(x.IrrigationSectorId)).GroupBy(x => x.IrrigationSectorId)
            .Select(group => new ConsumptionAggregate(group.Key, group.Count(), group.Count(x => x.VolumeLiters != null),
                group.Sum(x => x.VolumeLiters ?? 0m), group.Any(x => x.VolumeLiters != null && x.VolumeSource == VolumeSource.Estimated),
                group.Count(x => x.Status == IrrigationStatus.Completed), group.Count(x => x.Status == IrrigationStatus.Interrupted),
                group.Sum(x => x.ActiveDurationMinutes ?? 0m), group.Count(x => x.ActiveDurationMinutes != null)))
            .ToDictionaryAsync(x => x.SectorId, cancellationToken);
        var items = sectorPage.Select(sector =>
        {
            var coverage = Coverage(pageAggregates.GetValueOrDefault(sector.Id));
            var execution = Execution(pageAggregates.GetValueOrDefault(sector.Id));
            return new SectorConsumptionResponse(sector.Id, sector.Code, sector.Name, coverage.KnownVolumeLiters,
                coverage.EventCount, coverage.EventsWithKnownVolume, coverage.EventsWithoutVolume,
                coverage.Availability, coverage.IncludesEstimatedVolume, execution.CompletedCount, execution.InterruptedCount,
                execution.KnownActiveDurationMinutes, execution.EventsWithKnownDuration, execution.EventsWithoutDuration, execution.DurationAvailability);
        }).ToArray();
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<WaterConsumptionResponse>.Success(new(range.From, range.To, LocalPeriod.TimeZoneId,
            "L", request.SectorId, "Soma dos volumes informados em eventos válidos, pelo dia local de início; sem rateio por dia ou canteiro.",
            Coverage(aggregate), new(items, pagination.Page, pagination.PageSize, count))
        { ExecutionSummary = Execution(aggregate) }), items.Length);
    }

    private static WaterCoverage Coverage(ConsumptionAggregate? aggregate)
    {
        if (aggregate is null) return new(null, 0, 0, 0, ConsumptionAvailability.NoData, false);
        return new(aggregate.KnownCount == 0 ? null : aggregate.KnownVolume, aggregate.EventCount,
            aggregate.KnownCount, aggregate.EventCount - aggregate.KnownCount,
            aggregate.KnownCount == 0 ? ConsumptionAvailability.Unavailable :
                aggregate.KnownCount < aggregate.EventCount ? ConsumptionAvailability.Partial : ConsumptionAvailability.Available,
            aggregate.Estimated);
    }

    private static WaterExecutionCoverage Execution(ConsumptionAggregate? aggregate) => aggregate is null
        ? new(0, 0, null, 0, 0, ConsumptionAvailability.NoData)
        : new(aggregate.CompletedCount, aggregate.InterruptedCount,
            aggregate.DurationCount == 0 ? null : aggregate.KnownDuration, aggregate.DurationCount, aggregate.EventCount - aggregate.DurationCount,
            aggregate.DurationCount == 0 ? ConsumptionAvailability.Unavailable : aggregate.DurationCount < aggregate.EventCount ? ConsumptionAvailability.Partial : ConsumptionAvailability.Available);
}
