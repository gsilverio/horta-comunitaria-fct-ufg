using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetSoilMoisture;

internal sealed record GetSoilMoistureRequest(string? From, string? To, int Page, int PageSize, Guid? PointId, string? Unit, string? Method);

internal sealed record MoistureObservation(Guid Id, Guid PointId, DateTimeOffset CollectedAt, decimal Value, string Unit, string Method, MeasurementQuality Quality);
internal sealed record MoistureGroup(Guid PointId, string Unit, string Method, int Count, int NormalCount, int QuestionableCount, MoistureObservation? LastObservation);
internal sealed record GetSoilMoistureResponse(DateOnly From, DateOnly To, string TimeZone, string Definition, int NormalCount, int QuestionableCount, IndicatorAvailability Availability, Page<MoistureObservation> Origins, Page<MoistureGroup> Groups);

internal sealed class GetSoilMoistureQueryHandler(AppDbContext db, ILogger<GetSoilMoistureQueryHandler> logger)
{
    public async Task<Result<GetSoilMoistureResponse>> HandleAsync(Guid gardenId, GetSoilMoistureRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetSoilMoisture", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetSoilMoistureResponse> Reject(Error error) => process.Complete(Result<GetSoilMoistureResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.PointId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        var variableIds = db.EnvironmentalVariables.Where(x => x.IsSoilMoisture).Select(x => x.Id);
        var query = db.EnvironmentalMeasurements.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && variableIds.Contains(x.VariableId) && x.CollectedAt >= range.StartUtc && x.CollectedAt < range.EndUtc && (request.PointId == null || x.SamplingPointId == request.PointId) && (request.Unit == null || x.Unit == request.Unit.Trim()) && (request.Method == null || x.Method == request.Method.Trim()));
        var count = await query.CountAsync(cancellationToken);
        var normal = await query.CountAsync(x => x.Quality == MeasurementQuality.Normal, cancellationToken);
        var items = await query.OrderByDescending(x => x.CollectedAt).ThenByDescending(x => x.RecordedAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new MoistureObservation(x.Id, x.SamplingPointId, x.CollectedAt, x.Value, x.Unit, x.Method, x.Quality)).ToListAsync(cancellationToken);
        var groupsQuery = query.GroupBy(x => new { x.SamplingPointId, x.Unit, x.Method });
        var groupCount = await groupsQuery.Select(g => g.Key).CountAsync(cancellationToken);
        var groupRows = await groupsQuery.Select(g => new { PointId = g.Key.SamplingPointId, g.Key.Unit, g.Key.Method, Count = g.Count(), Normal = g.Count(x => x.Quality == MeasurementQuality.Normal), Questionable = g.Count(x => x.Quality == MeasurementQuality.Questionable) }).OrderBy(x => x.PointId).ThenBy(x => x.Unit).ThenBy(x => x.Method).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var pointIds = groupRows.Select(x => x.PointId).Distinct().ToArray();
        var lastRows = await query.Where(x => pointIds.Contains(x.SamplingPointId))
            .Where(x => !query.Any(y => y.SamplingPointId == x.SamplingPointId && y.Unit == x.Unit && y.Method == x.Method && (y.CollectedAt > x.CollectedAt || y.CollectedAt == x.CollectedAt && (y.RecordedAt > x.RecordedAt || y.RecordedAt == x.RecordedAt && y.Id.CompareTo(x.Id) > 0))))
            .Join(groupsQuery.Select(g => new { PointId = g.Key.SamplingPointId, g.Key.Unit, g.Key.Method }).OrderBy(x => x.PointId).ThenBy(x => x.Unit).ThenBy(x => x.Method).Skip(pagination.Skip).Take(pagination.PageSize), x => new { PointId = x.SamplingPointId, x.Unit, x.Method }, g => g, (x, g) => new MoistureObservation(x.Id, x.SamplingPointId, x.CollectedAt, x.Value, x.Unit, x.Method, x.Quality)).ToListAsync(cancellationToken);
        var lastByGroup = lastRows.ToDictionary(x => (x.PointId, x.Unit, x.Method));
        var groups = groupRows.Select(x => new MoistureGroup(x.PointId, x.Unit, x.Method, x.Count, x.Normal, x.Questionable, lastByGroup.GetValueOrDefault((x.PointId, x.Unit, x.Method)))).ToList();
        var response = new GetSoilMoistureResponse(range.From, range.To, LocalPeriod.TimeZoneId, "Observações válidas e última do recorte por ponto, unidade e método; sem média ou umidade atual presumida.", normal, count - normal, IndicatorCoverage.Availability(count, count), new(items, pagination.Page, pagination.PageSize, count), new(groups, pagination.Page, pagination.PageSize, groupCount));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetSoilMoistureResponse>.Success(response), items.Count);
    }
}
