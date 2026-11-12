using System.Globalization;
using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.ListGardenCrops;

/// <summary>Cultivo vigente na data da cena. <c>SpeciesImageUrl</c> aponta para a imagem ilustrativa da espécie, quando cadastrada.</summary>
internal sealed record GardenCropResponse(Guid CycleId, Guid BedId, Guid SpeciesId, string SpeciesName, string? SpeciesCatalogKey,
    string? ScientificName, string? SpeciesImageUrl, string? CultivarName, string? CultivarCatalogKey, decimal OccupiedAreaM2,
    long? InitialQuantity, bool HasReplacements, bool HasMixedPlantingDates, DateOnly? FirstPlantedOn,
    DateOnly? LastHarvestedOn, int? VisualGrowthDays, int? VisualHarvestDays, CropVisualStage Stage);

internal sealed class ListGardenCropsQueryHandler(AppDbContext db, ILogger<ListGardenCropsQueryHandler> logger)
{
    public async Task<Result<Page<GardenCropResponse>>> HandleAsync(Guid gardenId, string on, PaginationRequest pagination, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.ListGardenCrops");
        if (!DateOnly.TryParseExact(on, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return process.Complete(Result<Page<GardenCropResponse>>.Failure(Error.Validation("on", "Informe a data da cena em YYYY-MM-DD.")));
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return process.Complete(Result<Page<GardenCropResponse>>.Failure(valid.Error));
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await db.Gardens.AsNoTracking().AnyAsync(x => x.Id == gardenId, cancellationToken))
            return process.Complete(Result<Page<GardenCropResponse>>.Failure(Error.NotFound()));
        process.Step("QueryingCrops", "Consultando cultivos vigentes e seus plantios e colheitas válidos na data da cena");
        var query = from cycle in db.CultivationCycles.AsNoTracking()
                    join species in db.Species.AsNoTracking() on cycle.SpeciesId equals species.Id
                    join cultivar in db.Cultivars.AsNoTracking() on cycle.CultivarId equals (Guid?)cultivar.Id into cultivars
                    from cultivar in cultivars.DefaultIfEmpty()
                    where cycle.GardenId == gardenId && cycle.Validity == RecordValidity.Valid && cycle.StartedOn <= date &&
                        (cycle.EndedOn == null || cycle.EndedOn > date)
                    select new { cycle.Id, cycle.BedId, cycle.OccupiedAreaM2, SpeciesId = species.Id, species.CommonName, species.ScientificName,
                        SpeciesHasImage = species.ImageBase64 != null,
                        SpeciesCatalogKey = species.CatalogKey, CultivarName = cultivar == null ? null : cultivar.Name,
                        CultivarCatalogKey = cultivar == null ? null : cultivar.CatalogKey,
                        GrowthDays = cultivar != null && cultivar.VisualGrowthDays != null ? cultivar.VisualGrowthDays : species.VisualGrowthDays,
                        HarvestDays = cultivar != null && cultivar.VisualHarvestDays != null ? cultivar.VisualHarvestDays : species.VisualHarvestDays };
        var count = await query.CountAsync(cancellationToken);
        var cycles = await query.OrderBy(x => x.BedId).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var ids = cycles.Select(x => x.Id).ToArray();
        var plantings = await db.Plantings.AsNoTracking().Where(x => x.GardenId == gardenId && ids.Contains(x.CycleId) && x.Validity == RecordValidity.Valid && x.PlantedOn <= date)
            .GroupBy(x => x.CycleId).Select(g => new { CycleId = g.Key,
                InitialQuantity = g.Where(x => x.Kind == PlantingKind.Initial).Sum(x => (long)x.PlantedQuantity),
                First = g.Where(x => x.Kind == PlantingKind.Initial).Min(x => (DateOnly?)x.PlantedOn),
                Mixed = g.Select(x => x.PlantedOn).Distinct().Count() > 1,
                Replacements = g.Any(x => x.Kind == PlantingKind.Replacement) }).ToDictionaryAsync(x => x.CycleId, cancellationToken);
        var harvests = await db.Harvests.AsNoTracking().Where(x => x.GardenId == gardenId && ids.Contains(x.CycleId) && x.Validity == RecordValidity.Valid && x.HarvestedOn <= date && x.FreshMassKg > 0)
            .GroupBy(x => x.CycleId).Select(g => new { CycleId = g.Key, Last = g.Max(x => x.HarvestedOn) }).ToDictionaryAsync(x => x.CycleId, cancellationToken);
        var items = cycles.Select(cycle =>
        {
            plantings.TryGetValue(cycle.Id, out var planting);
            harvests.TryGetValue(cycle.Id, out var harvest);
            var stage = CropVisualAppearance.Resolve(date, planting?.First, harvest is not null, planting?.Mixed ?? false,
                planting?.Replacements ?? false, cycle.GrowthDays, cycle.HarvestDays);
            return new GardenCropResponse(cycle.Id, cycle.BedId, cycle.SpeciesId, cycle.CommonName, cycle.SpeciesCatalogKey, cycle.ScientificName,
                cycle.SpeciesHasImage ? $"/api/v1/species/{cycle.SpeciesId}/image" : null, cycle.CultivarName, cycle.CultivarCatalogKey, cycle.OccupiedAreaM2,
                planting?.First is null ? null : planting.InitialQuantity, planting?.Replacements ?? false, planting?.Mixed ?? false,
                planting?.First, harvest?.Last, cycle.GrowthDays, cycle.HarvestDays, stage);
        }).ToArray();
        await transaction.CommitAsync(cancellationToken);
        return process.Complete(Result<Page<GardenCropResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Length);
    }
}
