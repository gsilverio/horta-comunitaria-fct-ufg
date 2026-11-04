using System.Data;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Time;
using HortaComunitaria.Api.Features.Indicators.Shared;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Indicators.GetSocialEducationalParticipation;

internal sealed record GetSocialEducationalParticipationRequest(string? From, string? To, int Page, int PageSize, string? Dimension, Guid? InstitutionId);

internal sealed record AudienceOrigin(Guid ActivityId, DateOnly PerformedOn, bool IsSocial, bool IsEducational, int NominalPresences, int? KnownAudience, bool? AudienceListComplete);
internal sealed record GetSocialEducationalParticipationResponse(DateOnly From, DateOnly To, string Definition, int ActivityCount, int SocialCount, int EducationalCount, int? KnownAudience, int EventsWithoutAudienceTotal, int NominalPresences, int IdentifiedPeople, IndicatorAvailability Availability, Page<AudienceOrigin> Origins);

internal sealed class GetSocialEducationalParticipationQueryHandler(AppDbContext db, ILogger<GetSocialEducationalParticipationQueryHandler> logger)
{
    public async Task<Result<GetSocialEducationalParticipationResponse>> HandleAsync(Guid gardenId, GetSocialEducationalParticipationRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetSocialEducationalParticipation", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetSocialEducationalParticipationResponse> Reject(Error error) => process.Complete(Result<GetSocialEducationalParticipationResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.InstitutionId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        if (request.Dimension is not null and not "Social" and not "Educational") return Reject(Error.Validation("dimension", "Use Social ou Educational."));
        var activities = db.Activities.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.PerformedOn >= range.From && x.PerformedOn <= range.To && (x.IsSocial || x.IsEducational) && (request.Dimension == null || request.Dimension == "Social" && x.IsSocial || request.Dimension == "Educational" && x.IsEducational) && (request.InstitutionId == null || x.Institutions.Any(i => i.InstitutionId == request.InstitutionId)));
        var query = activities.Select(x => new { ActivityId = x.Id, x.PerformedOn, x.IsSocial, x.IsEducational, NominalPresences = x.Participants.Count(p => p.CountsAsAudience), KnownAudience = x.ReportedAudienceTotal != null ? x.ReportedAudienceTotal : x.AudienceListComplete == true ? x.Participants.Count(p => p.CountsAsAudience) : (int?)null, x.AudienceListComplete });
        var count = await query.CountAsync(cancellationToken);
        var known = await query.CountAsync(x => x.KnownAudience != null, cancellationToken);
        var audience = await query.SumAsync(x => x.KnownAudience, cancellationToken);
        var nominal = await query.SumAsync(x => x.NominalPresences, cancellationToken);
        var people = await activities.SelectMany(x => x.Participants).Where(x => x.CountsAsAudience).Select(x => x.ParticipantId).Distinct().CountAsync(cancellationToken);
        var social = await activities.CountAsync(x => x.IsSocial, cancellationToken);
        var educational = await activities.CountAsync(x => x.IsEducational, cancellationToken);
        var items = await query.OrderByDescending(x => x.PerformedOn).ThenBy(x => x.ActivityId).Skip(pagination.Skip).Take(pagination.PageSize).Select(x => new AudienceOrigin(x.ActivityId, x.PerformedOn, x.IsSocial, x.IsEducational, x.NominalPresences, x.KnownAudience, x.AudienceListComplete)).ToListAsync(cancellationToken);
        var response = new GetSocialEducationalParticipationResponse(range.From, range.To, "Público conhecido por evento inclui presenças nominais; pessoas identificadas distintas são somente a cobertura nominal. Ação multidimensional conta uma vez no total geral.", count, social, educational, known == 0 ? null : audience, count - known, nominal, people, IndicatorCoverage.Availability(count, known), new(items, pagination.Page, pagination.PageSize, count));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetSocialEducationalParticipationResponse>.Success(response), items.Count);
    }
}
