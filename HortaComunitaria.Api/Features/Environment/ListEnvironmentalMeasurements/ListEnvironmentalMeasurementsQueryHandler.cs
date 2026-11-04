using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

using HortaComunitaria.Api.Features.Environment.Shared;
namespace HortaComunitaria.Api.Features.Environment.ListEnvironmentalMeasurements;

internal sealed record ListEnvironmentalMeasurementsRequest(string? From, string? To, int Page, int PageSize, string? Validity, Guid? PointId, Guid? VariableId, string? Unit, string? Method, string? Quality);
internal sealed class ListEnvironmentalMeasurementsQueryHandler(AppDbContext db, ILogger<ListEnvironmentalMeasurementsQueryHandler> logger)
{
    public async Task<Result<Page<EnvironmentalMeasurementResponse>>> HandleAsync(Guid gardenId, ListEnvironmentalMeasurementsRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Environment.ListEnvironmentalMeasurements", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<Page<EnvironmentalMeasurementResponse>> Reject(Error error) => process.Complete(Result<Page<EnvironmentalMeasurementResponse>>.Failure(error));
        if (gardenId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return Reject(valid.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error with { Code = "record.invalid_period" });
        var validity = EnumFilter.Parse<RecordValidity>(request.Validity, "validity");
        if (!validity.IsSuccess) return Reject(validity.Error);
        if (request.PointId == Guid.Empty) return Reject(Error.Validation("pointId", "Identificador inválido."));
        if (request.VariableId == Guid.Empty) return Reject(Error.Validation("variableId", "Identificador inválido."));
        var unit = RecordRules.Optional(request.Unit);
        var unitError = RecordRules.Text(unit, "unit", 120);
        if (unitError is not null) return Reject(unitError);
        var method = RecordRules.Optional(request.Method);
        var methodError = RecordRules.Text(method, "method", 200);
        if (methodError is not null) return Reject(methodError);
        var quality = EnumFilter.Parse<MeasurementQuality>(request.Quality, "quality");
        if (!quality.IsSuccess) return Reject(quality.Error);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        process.Step("Querying", "Consultando fatos do período e filtros antes da paginação");
        var query = db.EnvironmentalMeasurements.AsNoTracking().Where(x => x.GardenId == gardenId && x.CollectedAt >= period.Value.StartUtc && x.CollectedAt < period.Value.EndUtc && (validity.Value.Value == null || x.Validity == validity.Value.Value) && (request.PointId == null || x.SamplingPointId == request.PointId) && (request.VariableId == null || x.VariableId == request.VariableId) && (unit == null || x.Unit == unit) && (method == null || x.Method == method) && (quality.Value.Value == null || x.Quality == quality.Value.Value));
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CollectedAt).ThenByDescending(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).Select(EnvironmentalMeasurementProjection.Select).ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<EnvironmentalMeasurementResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
