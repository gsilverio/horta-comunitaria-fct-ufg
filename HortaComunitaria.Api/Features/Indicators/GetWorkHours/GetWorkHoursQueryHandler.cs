using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetWorkHours;

internal sealed record GetWorkHoursRequest(string? From, string? To, int Page, int PageSize, Guid? ParticipantId, string? Category);

internal sealed record WorkOrigin(Guid ActivityId, DateOnly PerformedOn, Guid ParticipantId, decimal? WorkHours);
internal sealed record GetWorkHoursResponse(DateOnly From, DateOnly To, string Unit, decimal? KnownHours, int ActivityCount, int KnownParticipationCount, int UnknownParticipationCount, int ActivitiesWithoutParticipants, IndicatorAvailability Availability, Page<WorkOrigin> Origins);

internal sealed class GetWorkHoursQueryHandler(AppDbContext db, ILogger<GetWorkHoursQueryHandler> logger)
{
    public async Task<Result<GetWorkHoursResponse>> HandleAsync(Guid gardenId, GetWorkHoursRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetWorkHours", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetWorkHoursResponse> Reject(Error error) => process.Complete(Result<GetWorkHoursResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.ParticipantId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        var activities = db.Activities.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.PerformedOn >= range.From && x.PerformedOn <= range.To && (request.Category == null || x.Category == request.Category.Trim()) && (request.ParticipantId == null || x.Participants.Any(p => p.ParticipantId == request.ParticipantId)));
        var query = from a in activities from p in a.Participants where request.ParticipantId == null || p.ParticipantId == request.ParticipantId select new { ActivityId = a.Id, a.PerformedOn, p.ParticipantId, p.WorkHours };
        var activityCount = await activities.CountAsync(cancellationToken);
        var empty = await activities.CountAsync(x => !x.Participants.Any(), cancellationToken);
        var count = await query.CountAsync(cancellationToken);
        var known = await query.CountAsync(x => x.WorkHours != null, cancellationToken);
        var hours = await query.SumAsync(x => x.WorkHours, cancellationToken);
        var items = await query.OrderByDescending(x => x.PerformedOn).ThenBy(x => x.ActivityId).ThenBy(x => x.ParticipantId).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new WorkOrigin(x.ActivityId, x.PerformedOn, x.ParticipantId, x.WorkHours)).ToListAsync(cancellationToken);
        var response = new GetWorkHoursResponse(range.From, range.To, "h", known == 0 ? null : hours, activityCount, known, count - known, empty, activityCount == 0 ? IndicatorAvailability.NoData : IndicatorCoverage.Availability(count + empty, known), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetWorkHoursResponse>.Success(response), items.Count);
    }
}
