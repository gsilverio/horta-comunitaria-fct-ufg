using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Irrigation.ListIrrigations;

/// <summary>Filtros locais inclusivos; validade usa nomes textuais e rejeita números.</summary>
internal sealed record ListIrrigationsRequest(string? From, string? To, Guid? SectorId, string? Validity, int Page, int PageSize);
/// <summary>Resumo do fato, sem carregar canteiros ou histórico.</summary>
internal sealed record IrrigationSummaryResponse(Guid Id, Guid SectorId, string SectorName, DateTimeOffset StartedAt,
    decimal? VolumeLiters, IrrigationStatus Status, RecordValidity Validity, int Version);

internal sealed class ListIrrigationsQueryHandler(AppDbContext db, ILogger<ListIrrigationsQueryHandler> logger)
{
    public async Task<Result<Page<IrrigationSummaryResponse>>> HandleAsync(Guid gardenId, ListIrrigationsRequest request,
        CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Irrigation.ListIrrigations",
            new Dictionary<string, object?> { ["GardenId"] = gardenId, ["SectorId"] = request.SectorId });
        Result<Page<IrrigationSummaryResponse>> Reject(Error error) => process.Complete(Result<Page<IrrigationSummaryResponse>>.Failure(error));
        var pagination = new PaginationRequest(request.Page, request.PageSize);
        var validation = pagination.Validate();
        if (!validation.IsSuccess) return Reject(validation.Error);
        var period = LocalPeriod.Create(request.From, request.To);
        if (!period.IsSuccess) return Reject(period.Error);
        var validityName = request.Validity ?? nameof(RecordValidity.Valid);
        if (!Enum.GetNames<RecordValidity>().Contains(validityName, StringComparer.Ordinal))
            return Reject(Error.Validation("validity", "Use Valid ou Invalid como texto."));
        var validity = Enum.Parse<RecordValidity>(validityName);
        if (gardenId == Guid.Empty || request.SectorId == Guid.Empty)
            return Reject(Error.Validation("id", "Identificadores devem ser não vazios."));
        process.Step("CheckingContext", "Conferindo horta e filtro de setor para consulta de irrigações");
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken) ||
            request.SectorId is not null && !await db.IrrigationSectors.AsNoTracking().AnyAsync(
                x => x.Id == request.SectorId && x.GardenId == gardenId, cancellationToken))
            return Reject(Error.NotFound());
        var range = period.Value;
        process.Step("Querying", "Consultando irrigações de {From} a {To}, validade {Validity}, página {Page}", range.From, range.To, validity, pagination.Page);
        var query = from irrigation in db.Irrigations.AsNoTracking()
                    join sector in db.IrrigationSectors.AsNoTracking() on irrigation.IrrigationSectorId equals sector.Id
                    where irrigation.GardenId == gardenId && irrigation.StartedAt >= range.StartUtc && irrigation.StartedAt < range.EndUtc &&
                        irrigation.Validity == validity && (request.SectorId == null || irrigation.IrrigationSectorId == request.SectorId)
                    select new
                    {
                        irrigation.Id,
                        SectorId = sector.Id,
                        SectorName = sector.Name,
                        irrigation.StartedAt,
                        irrigation.VolumeLiters,
                        irrigation.Status,
                        irrigation.Validity,
                        irrigation.Version
                    };
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.StartedAt).ThenByDescending(x => x.Id)
            .Skip(pagination.Skip).Take(pagination.PageSize)
            .Select(x => new IrrigationSummaryResponse(x.Id, x.SectorId, x.SectorName, x.StartedAt,
                x.VolumeLiters, x.Status, x.Validity, x.Version)).ToListAsync(cancellationToken);
        return process.Complete(Result<Page<IrrigationSummaryResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Count);
    }
}
