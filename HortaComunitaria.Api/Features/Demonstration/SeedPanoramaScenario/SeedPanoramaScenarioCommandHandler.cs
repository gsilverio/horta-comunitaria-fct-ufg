using System.Security.Claims;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Features.Activities.CreateParticipant;
using HortaComunitaria.Api.Features.Cultivation.ConfigureSpeciesVisualGrowth;
using HortaComunitaria.Api.Features.Cultivation.OpenCultivationCycle;
using HortaComunitaria.Api.Features.Cultivation.RegisterHarvest;
using HortaComunitaria.Api.Features.Cultivation.RegisterPlanting;
using HortaComunitaria.Api.Features.Gardens.AssignBedToSector;
using HortaComunitaria.Api.Features.Gardens.ConfigureBed;
using HortaComunitaria.Api.Features.Gardens.CreateBed;
using HortaComunitaria.Api.Features.Gardens.CreateGarden;
using HortaComunitaria.Api.Features.Gardens.CreateIrrigationSector;
using HortaComunitaria.Api.Features.Gardens.SaveLayout;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Demonstration.SeedPanoramaScenario;

/// <summary>
/// Segunda parte do comando explícito <c>--seed-demo</c>: horta fictícia para apresentar o mapa e o panorama
/// (versão 1.0.0 em planejamento). Canteiros de várias formas (retangulares, girados, curvos da mandala e circulares,
/// em setor de contorno livre) com espécies do catálogo Embrapa em fases distintas: aguardando plantio, início,
/// crescimento, colheita prevista, colheita registrada e fase conjunta por reposição. As fases saem das datas de plantio
/// e dos prazos visuais da espécie; quando a espécie ainda não tem prazos, o comando os configura com motivo.
/// Usa os handlers reais, como o cenário de validação, e não grava nada se a horta já existir.
/// </summary>
internal sealed class SeedPanoramaScenarioCommandHandler(
    AppDbContext db, IHttpContextAccessor http, TimeProvider clock,
    CreateGardenCommandHandler createGarden, CreateIrrigationSectorCommandHandler createSector, CreateBedCommandHandler createBed,
    AssignBedToSectorCommandHandler assignBed, ConfigureBedCommandHandler configureBed, SaveLayoutCommandHandler saveLayout,
    CreateParticipantCommandHandler createParticipant, ConfigureSpeciesVisualGrowthCommandHandler configureVisual,
    OpenCultivationCycleCommandHandler openCycle, RegisterPlantingCommandHandler registerPlanting, RegisterHarvestCommandHandler registerHarvest,
    RegisterIrrigationCommandHandler registerIrrigation, ILogger<SeedPanoramaScenarioCommandHandler> logger)
{
    /// <summary>Código estável da horta panorâmica; sua existência torna o comando idempotente.</summary>
    internal const string GardenCode = "DEMO-PANORAMA";

    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(-3);

    /// <summary>Prazos visuais (dias até crescimento e até colheita) usados só quando a espécie ainda não tem os seus.</summary>
    private static readonly Dictionary<string, (int Growth, int Harvest)> VisualDefaults = new()
    {
        ["alface"] = (20, 45), ["tomate"] = (35, 90), ["cenoura"] = (30, 85), ["pimenta-annuum"] = (35, 100), ["mostarda"] = (15, 40),
        ["abobora"] = (35, 100), ["berinjela"] = (35, 100), ["brocolis"] = (30, 90), ["cebola"] = (40, 120), ["alho"] = (50, 150),
        ["batata-doce"] = (40, 120), ["repolho"] = (30, 90), ["pepino"] = (25, 55), ["milho-doce"] = (35, 90), ["melao"] = (30, 80),
        ["ervilha"] = (25, 70),
    };

    private enum Phase { Awaiting, Seedling, Growing, HarvestExpected, HarvestRecorded, Mixed }

    /// <summary>Retorna 0 em sucesso ou quando já aplicado e 1 quando faltam pré-requisitos.</summary>
    internal async Task<int> RunAsync(CancellationToken token)
    {
        using var process = new ProcessLog(logger, "Demonstration.SeedPanoramaScenario");
        process.Step("CheckingPrerequisites", "Conferindo administrador, catálogo Embrapa e horta panorâmica");
        if (await db.Gardens.AnyAsync(x => x.Code == GardenCode, token))
        {
            process.Event("PanoramaScenarioAlreadyApplied", "Skipped", "Horta panorâmica {GardenCode} já existe; nada foi gravado", GardenCode);
            return 0;
        }
        var actor = await db.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDemonstration && x.Profile == UserProfile.Administrator)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(token);
        if (actor is null) return Reject(process, "Nenhum administrador ativo; execute --bootstrap-admin antes.");
        var keys = VisualDefaults.Keys.Select(x => "embrapa-2023:" + x).ToArray();
        var catalog = await db.Species.AsNoTracking().Where(x => x.CatalogKey != null && keys.Contains(x.CatalogKey) && x.IsActive)
            .Select(x => new { Key = x.CatalogKey!, x.Id, x.Version, x.VisualGrowthDays, x.VisualHarvestDays }).ToListAsync(token);
        if (catalog.Count != VisualDefaults.Count) return Reject(process, "Catálogo Embrapa ausente; execute --seed-embrapa antes.");

        http.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.Value.ToString())], "seed-demo"))
        };
        var today = LocalPeriod.LocalDate(clock.GetUtcNow());
        var structureStart = today.AddDays(-200);

        process.Step("ConfiguringVisualGrowth", "Configurando prazos visuais das espécies que ainda não os têm");
        var species = new Dictionary<string, (Guid Id, int Growth, int Harvest)>();
        foreach (var item in catalog)
        {
            var key = item.Key["embrapa-2023:".Length..];
            var (growth, harvest) = VisualDefaults[key];
            if (item.VisualGrowthDays is { } g && item.VisualHarvestDays is { } h) (growth, harvest) = (g, h);
            else Ok(await configureVisual.HandleAsync(item.Id, new(growth, harvest, item.Version,
                "Prazos visuais de referência para o panorama demonstrativo (ajustáveis no catálogo)."), token));
            species[key] = (item.Id, growth, harvest);
        }

        process.Step("CreatingStructure", "Criando horta, setores e canteiros de formas variadas");
        var garden = Ok(await createGarden.HandleAsync(new(GardenCode, "Horta panorâmica (dados fictícios)",
            "Horta fictícia para demonstrar o mapa e o panorama dos cultivos; não representa a horta real da FCT/UFG.", 1120m, 900m, 120m), token)).Id;
        var rows = Ok(await createSector.HandleAsync(garden, new("PAN-S1", "Setor dos canteiros retos (fictício)", "Canteiros retangulares, inclusive girados."), token));
        var mandala = Ok(await createSector.HandleAsync(garden, new("PAN-S2", "Setor mandala (fictício)", "Canteiros curvos em dois anéis ao redor do centro."), token));
        var circles = Ok(await createSector.HandleAsync(garden, new("PAN-S3", "Setor dos canteiros circulares (fictício)", "Contorno livre com canteiros redondos."), token));
        var sectorIds = new Dictionary<string, IrrigationSectorResponse> { ["s1"] = rows, ["s2"] = mandala, ["s3"] = circles };
        var participant = Ok(await createParticipant.HandleAsync(garden, new("Gabriela (colaboradora fictícia)", "Estudante"), token)).Id;
        var helper = Ok(await createParticipant.HandleAsync(garden, new("Heitor (colaborador fictício)", "Voluntário"), token)).Id;

        var layout = new List<LayoutElement>
        {
            new("s1", rows.Name, "sector", "rectangle", rows.Id, X: 1, Y: 1, Width: 18, Height: 12, RotationDeg: 0),
            new("s2", mandala.Name, "sector", "rectangle", mandala.Id, X: 21, Y: 1, Width: 18, Height: 18, RotationDeg: 0),
            new("s3", circles.Name, "sector", "rectangle", circles.Id, X: 1, Y: 15, Width: 18, Height: 12, RotationDeg: 0,
                Outline: [new(1, 15), new(19, 15), new(19, 26), new(10, 27), new(1, 26)]),
            new("ref-mandala", "Centro da mandala (referência)", "reference", "circle", CenterX: 30, CenterY: 10, Radius: 1.6),
            new("ref-water", "Caixa d'água (referência fictícia)", "reference", "circle", CenterX: 36, CenterY: 24, Radius: 1.5),
            new("ref-compost", "Composteira (referência fictícia)", "reference", "rectangle", X: 23, Y: 21, Width: 4, Height: 3, RotationDeg: 0),
        };
        var beds = new Dictionary<string, (Guid Id, decimal Area)>();
        async Task Bed(string code, string name, string sector, decimal area, decimal? length, decimal? width, LayoutElement shape)
        {
            var bed = Ok(await createBed.HandleAsync(garden, new("PAN-" + code, name, null), token));
            var assigned = Ok(await assignBed.HandleAsync(garden, bed.Id, new(sectorIds[sector].Id, structureStart, bed.Version, "Configuração inicial fictícia"), token));
            Ok(await configureBed.HandleAsync(garden, bed.Id, new(area, length, width, structureStart, assigned.BedVersion, "Configuração inicial fictícia"), token));
            beds[code] = (bed.Id, area);
            layout.Add(shape with { Id = code, Name = name, Role = "bed", EntityId = bed.Id, SectorId = sector });
        }
        LayoutElement Rect(double x, double y, double w, double h, double rotation = 0) => new("", "", "bed", "rectangle", X: x, Y: y, Width: w, Height: h, RotationDeg: rotation);
        LayoutElement Circle(double cx, double cy, double r) => new("", "", "bed", "circle", CenterX: cx, CenterY: cy, Radius: r);
        // Mesma construção dos canteiros curvos da tela: retângulo de apoio tangente ao anel e parâmetros da mandala.
        LayoutElement Curved(double ring, double angle, double angularWidth, double radial)
        {
            var radians = angle * Math.PI / 180;
            var tangential = 2 * Math.PI * ring * angularWidth / 360;
            var cx = 30 + Math.Cos(radians) * ring; var cy = 10 + Math.Sin(radians) * ring;
            return new("", "", "bed", "rectangle", X: cx - tangential / 2, Y: cy - radial / 2, Width: tangential, Height: radial, RotationDeg: ((angle - 90) % 360 + 360) % 360,
                Mandala: new("curved", 30, 10, ring, angle, angularWidth, radial));
        }
        decimal CurvedArea(double ring, double angularWidth, double radial) => Math.Round((decimal)(2 * Math.PI * ring * angularWidth / 360 * radial), 2);
        decimal CircleArea(double r) => Math.Round((decimal)(Math.PI * r * r), 2);

        await Bed("R1", "Canteiro reto 1 (fictício)", "s1", 9m, 6m, 1.5m, Rect(2, 2, 6, 1.5));
        await Bed("R2", "Canteiro reto 2 (fictício)", "s1", 9m, 6m, 1.5m, Rect(2, 5, 6, 1.5));
        await Bed("R3", "Canteiro reto 3 (fictício)", "s1", 9m, 6m, 1.5m, Rect(2, 8, 6, 1.5));
        await Bed("R4", "Canteiro girado (fictício)", "s1", 7m, 5m, 1.4m, Rect(10.5, 3, 5, 1.4, 30));
        await Bed("R5", "Canteiro largo (fictício)", "s1", 12.5m, 5m, 2.5m, Rect(11, 8.5, 5, 2.5));
        foreach (var (angle, index) in new[] { 0d, 90, 180, 270 }.Select((x, i) => (x, i + 1)))
            await Bed($"M1-{index}", $"Mandala · anel interno {index} (fictício)", "s2", CurvedArea(4.5, 70, 2), null, null, Curved(4.5, angle, 70, 2));
        foreach (var (angle, index) in new[] { 30d, 90, 150, 210, 270, 330 }.Select((x, i) => (x, i + 1)))
            await Bed($"M2-{index}", $"Mandala · anel externo {index} (fictício)", "s2", CurvedArea(7.2, 45, 1.8), null, null, Curved(7.2, angle, 45, 1.8));
        await Bed("C1", "Canteiro redondo 1 (fictício)", "s3", CircleArea(1.8), null, null, Circle(5, 19, 1.8));
        await Bed("C2", "Canteiro redondo grande (fictício)", "s3", CircleArea(2.2), null, null, Circle(10, 20.5, 2.2));
        await Bed("C3", "Canteiro redondo 3 (fictício)", "s3", CircleArea(1.8), null, null, Circle(15, 19, 1.8));
        await Bed("C4", "Canteiro redondo pequeno 1 (fictício)", "s3", CircleArea(1.2), null, null, Circle(6.5, 24.3, 1.2));
        await Bed("C5", "Canteiro redondo pequeno 2 (fictício)", "s3", CircleArea(1.2), null, null, Circle(13.5, 24.3, 1.2));

        process.Step("SavingLayout", "Salvando o mapa com as formas dos canteiros");
        Ok(await saveLayout.HandleAsync(garden, new(Guid.CreateVersion7(), 0,
            new LayoutDocument(40, 28, [new(0, 0), new(40, 0), new(40, 28), new(0, 28)], [.. layout])), token));

        process.Step("RegisteringCrops", "Abrindo cultivos com datas de plantio que resultam em fases distintas");
        async Task Crop(string bed, string key, Phase phase, decimal share = 1m, int perSquareMeter = 4)
        {
            var (speciesId, growth, harvest) = species[key];
            // Idade do plantio em dias: o panorama compara com os prazos visuais da espécie.
            var age = phase switch
            {
                Phase.Seedling => Math.Max(3, growth / 2),
                Phase.Growing => (growth + harvest) / 2,
                Phase.HarvestExpected => harvest + 5,
                Phase.HarvestRecorded => harvest + 8,
                Phase.Mixed => (growth + harvest) / 2,
                _ => 10,
            };
            var plantedOn = today.AddDays(-age);
            var area = Math.Round(beds[bed].Area * share, 2);
            var responsible = phase is Phase.HarvestRecorded or Phase.Mixed ? participant : helper;
            var cycle = Ok(await openCycle.HandleAsync(garden, new(Guid.CreateVersion7(), beds[bed].Id, speciesId, null, plantedOn, area, responsible), token)).Detail;
            if (phase == Phase.Awaiting) return;
            var quantity = Math.Max(3, (int)Math.Round(area * perSquareMeter));
            var first = Ok(await registerPlanting.HandleAsync(garden, new(Guid.CreateVersion7(), cycle.Id, null, null, plantedOn, PlantingKind.Initial,
                quantity, null, null, responsible), token)).Detail.Id;
            if (phase == Phase.Mixed)
                Ok(await registerPlanting.HandleAsync(garden, new(Guid.CreateVersion7(), cycle.Id, null, first, plantedOn.AddDays(12), PlantingKind.Replacement,
                    Math.Max(1, quantity / 6), null, null, responsible), token));
            if (phase == Phase.HarvestRecorded)
                Ok(await registerHarvest.HandleAsync(garden, new(Guid.CreateVersion7(), cycle.Id, today.AddDays(-2), Math.Round(area * 0.6m, 2),
                    Math.Round(area * 0.55m, 2), null, participant), token));
        }

        await Crop("R1", "alface", Phase.Seedling, 0.5m, 8);
        await Crop("R1", "cebola", Phase.Growing, 0.5m, 10);
        await Crop("R2", "tomate", Phase.HarvestRecorded, perSquareMeter: 2);
        await Crop("R3", "cenoura", Phase.Growing, perSquareMeter: 20);
        await Crop("R4", "mostarda", Phase.HarvestExpected, perSquareMeter: 6);
        await Crop("R5", "abobora", Phase.Growing, perSquareMeter: 1);
        await Crop("M1-1", "pimenta-annuum", Phase.Growing, perSquareMeter: 2);
        await Crop("M1-2", "berinjela", Phase.Seedling, perSquareMeter: 2);
        await Crop("M1-3", "brocolis", Phase.HarvestExpected, perSquareMeter: 3);
        await Crop("M1-4", "repolho", Phase.Mixed, perSquareMeter: 3);
        await Crop("M2-1", "alface", Phase.HarvestRecorded, perSquareMeter: 8);
        await Crop("M2-2", "ervilha", Phase.Growing, perSquareMeter: 6);
        await Crop("M2-3", "pepino", Phase.Seedling, perSquareMeter: 2);
        await Crop("M2-4", "milho-doce", Phase.Growing, perSquareMeter: 4);
        await Crop("M2-5", "alho", Phase.Seedling, perSquareMeter: 12);
        await Crop("M2-6", "cebola", Phase.Awaiting);
        await Crop("C1", "batata-doce", Phase.Growing, perSquareMeter: 2);
        await Crop("C2", "melao", Phase.HarvestExpected, 0.5m, 1);
        await Crop("C2", "mostarda", Phase.Seedling, 0.5m, 6);
        await Crop("C3", "tomate", Phase.Seedling, perSquareMeter: 2);
        await Crop("C4", "alface", Phase.Growing, perSquareMeter: 8);
        // C5 fica livre para mostrar um canteiro sem cultivo.

        process.Step("RegisteringIrrigations", "Registrando irrigações manuais recentes por setor");
        foreach (var day in new[] { 9, 7, 5, 3, 1 })
            foreach (var (sector, codes, volume) in new[] { (rows, new[] { "R1", "R2", "R3", "R4", "R5" }, 120m), (mandala, beds.Keys.Where(x => x.StartsWith('M')).ToArray(), 150m),
                (circles, new[] { "C1", "C2", "C3", "C4" }, 70m) })
            {
                var start = new DateTimeOffset(today.AddDays(-day).ToDateTime(new TimeOnly(7, 0)), LocalOffset);
                Ok(await registerIrrigation.HandleAsync(garden, new(Guid.CreateVersion7(), sector.Id, [.. codes.Select(x => beds[x].Id)], start, start.AddMinutes(30),
                    30m, volume, null, VolumeSource.Estimated, "Regador e mangueira (estimativa)", IrrigationStatus.Completed, null, "Registro manual fictício."), token));
            }

        process.Event("PanoramaScenarioApplied", "Created", "Horta panorâmica {GardenId} criada com {BedCount} canteiros fictícios", garden, beds.Count);
        process.Complete(Result<Unit>.Success(new()));
        return 0;
    }

    private static T Ok<T>(Result<T> result) => result.IsSuccess ? result.Value
        : throw new InvalidOperationException($"Cenário panorâmico rejeitado: {result.Error.Code} — {result.Error.Message}");

    private static int Reject(ProcessLog process, string message)
    {
        process.Event("PanoramaScenarioRejected", "Rejected", "Cenário panorâmico não criado: {Reason}", message);
        return 1;
    }
}
