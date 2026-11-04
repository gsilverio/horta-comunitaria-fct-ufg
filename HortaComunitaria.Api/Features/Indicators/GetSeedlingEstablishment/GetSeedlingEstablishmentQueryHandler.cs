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

namespace HortaComunitaria.Api.Features.Indicators.GetSeedlingEstablishment;

internal sealed record GetSeedlingEstablishmentRequest(string? From, string? To, int Page, int PageSize, Guid? SpeciesId, string? Kind, string? Method);

internal sealed record EstablishmentOrigin(Guid PlantingId, PlantingKind Kind, int PlantedQuantity, Guid? AssessmentId, DateOnly? AssessedOn, int? SurvivingQuantity, string? Method, decimal? Percentage);
internal sealed record EstablishmentGroup(PlantingKind Kind, string? Method, int PlantingCount, int AssessedCount, long AssessedPlantedQuantity, long SurvivingQuantity, decimal? Percentage);
internal sealed record EstablishmentSelection(Guid Id, PlantingKind Kind, int PlantedQuantity, Guid? AssessmentId, DateOnly? AssessedOn, int? SurvivingQuantity, string? Method);
internal sealed record GetSeedlingEstablishmentResponse(DateOnly From, DateOnly To, string Definition, int EligiblePlantingCount, int IneligibleMaterialCount, int WithoutAssessmentCount, IndicatorAvailability Availability, Page<EstablishmentOrigin> Origins, Page<EstablishmentGroup> Groups);

internal sealed class GetSeedlingEstablishmentQueryHandler(AppDbContext db, ILogger<GetSeedlingEstablishmentQueryHandler> logger)
{
    public async Task<Result<GetSeedlingEstablishmentResponse>> HandleAsync(Guid gardenId, GetSeedlingEstablishmentRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Indicators.GetSeedlingEstablishment", new Dictionary<string, object?> { ["GardenId"] = gardenId });
        Result<GetSeedlingEstablishmentResponse> Reject(Error error) => process.Complete(Result<GetSeedlingEstablishmentResponse>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        if (gardenId == Guid.Empty || request.SpeciesId == Guid.Empty) return Reject(Error.Validation("id", "Identificador inválido."));
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken)) return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Aggregating", "Consultando origens válidas e cobertura de {From} a {To} no mesmo snapshot", range.From, range.To);

        var kind = EnumFilter.Parse<PlantingKind>(request.Kind, "kind");
        if (!kind.IsSuccess) return Reject(kind.Error);
        var cycleIds = db.CultivationCycles.Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && (request.SpeciesId == null || x.SpeciesId == request.SpeciesId)).Select(x => x.Id);
        var plantings = db.Plantings.AsNoTracking().Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.PlantedOn >= range.From && x.PlantedOn <= range.To && cycleIds.Contains(x.CycleId) && (kind.Value.Value == null || x.Kind == kind.Value.Value));
        var eligibleLotIds = db.PropagationLots.Where(x => x.GardenId == gardenId && x.MaterialType == MaterialType.Seedling).Select(x => x.Id);
        var eligible = plantings.Where(x => x.PropagationLotId != null && eligibleLotIds.Contains(x.PropagationLotId.Value));
        var ineligible = await plantings.CountAsync(cancellationToken) - await eligible.CountAsync(cancellationToken);
        var assessments = db.EstablishmentAssessments.Where(x => x.GardenId == gardenId && x.Validity == RecordValidity.Valid && x.AssessedOn <= range.To && (request.Method == null || x.Method == request.Method.Trim()));
        var query = from p in eligible
                    from a in assessments.Where(x => x.PlantingId == p.Id && x.AssessedOn >= p.PlantedOn).OrderByDescending(x => x.AssessedOn).ThenByDescending(x => x.RecordedAt).ThenByDescending(x => x.Id).Take(1).DefaultIfEmpty()
                    select new { p.Id, p.Kind, p.PlantedQuantity, AssessmentId = a == null ? (Guid?)null : a.Id, AssessedOn = a == null ? (DateOnly?)null : a.AssessedOn, SurvivingQuantity = a == null ? (int?)null : a.SurvivingQuantity, Method = a == null ? null : a.Method };
        var count = await query.CountAsync(cancellationToken);
        var known = await query.CountAsync(x => x.AssessmentId != null, cancellationToken);
        var rows = await query.OrderBy(x => x.Kind).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var items = rows.Select(x => new EstablishmentOrigin(x.Id, x.Kind, x.PlantedQuantity, x.AssessmentId, x.AssessedOn, x.SurvivingQuantity, x.Method, x.SurvivingQuantity == null ? null : IndicatorMath.Percentage(x.SurvivingQuantity.Value, x.PlantedQuantity))).ToList();
        var grouped = query.GroupBy(x => new { x.Kind, x.Method });
        var groupCount = await grouped.Select(g => g.Key).CountAsync(cancellationToken);
        var groupRows = await grouped.Select(g => new { g.Key.Kind, g.Key.Method, Count = g.Count(), Known = g.Count(x => x.AssessmentId != null), Planted = g.Sum(x => x.AssessmentId != null ? (long)x.PlantedQuantity : 0L), Surviving = g.Sum(x => (long)(x.SurvivingQuantity ?? 0)) }).OrderBy(x => x.Kind).ThenBy(x => x.Method).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var groups = groupRows.Select(x => new EstablishmentGroup(x.Kind, x.Method, x.Count, x.Known, x.Planted, x.Surviving, x.Known == 0 ? null : IndicatorMath.Percentage(x.Surviving, x.Planted))).ToList();
        var response = new GetSeedlingEstablishmentResponse(range.From, range.To, "Última avaliação válida até o fim do recorte, filtrando método antes da seleção. Percentuais ponderados por método e natureza; material desconhecido ou diferente de muda não entra na taxa.", count, ineligible, count - known, IndicatorCoverage.Availability(count, known), new(items, pagination.Page, pagination.PageSize, count), new(groups, pagination.Page, pagination.PageSize, groupCount));

        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<GetSeedlingEstablishmentResponse>.Success(response), items.Count);
    }
}
