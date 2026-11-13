using System.Security.Claims;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Evidence;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Features.Activities.CorrectActivity;
using HortaComunitaria.Api.Features.Activities.CreateInput;
using HortaComunitaria.Api.Features.Activities.InactivateParticipant;
using HortaComunitaria.Api.Features.Activities.InvalidateActivity;
using HortaComunitaria.Api.Features.Costs.CorrectFinancialEntry;
using HortaComunitaria.Api.Features.Costs.InvalidateFinancialEntry;
using HortaComunitaria.Api.Features.Costs.Shared;
using HortaComunitaria.Api.Features.Cultivation.ConfigureCultivarVisualGrowth;
using HortaComunitaria.Api.Features.Cultivation.CorrectAgronomicObservation;
using HortaComunitaria.Api.Features.Cultivation.CorrectEstablishmentAssessment;
using HortaComunitaria.Api.Features.Cultivation.CorrectPlanting;
using HortaComunitaria.Api.Features.Cultivation.CreateCultivar;
using HortaComunitaria.Api.Features.Cultivation.InactivatePropagationLot;
using HortaComunitaria.Api.Features.Cultivation.InvalidateAgronomicObservation;
using HortaComunitaria.Api.Features.Cultivation.InvalidateHarvest;
using HortaComunitaria.Api.Features.Environment.InvalidateEnvironmentalMeasurement;
using HortaComunitaria.Api.Features.Evidence.UploadEvidence;
using HortaComunitaria.Api.Features.Irrigation.CorrectIrrigation;
using HortaComunitaria.Api.Features.Irrigation.InvalidateIrrigation;
using HortaComunitaria.Api.Features.Occurrences.CorrectOccurrence;
using HortaComunitaria.Api.Features.Activities.CreateInstitution;
using HortaComunitaria.Api.Features.Activities.CreateParticipant;
using HortaComunitaria.Api.Features.Activities.RegisterActivity;
using HortaComunitaria.Api.Features.Costs.RegisterFinancialEntry;
using HortaComunitaria.Api.Features.Cultivation.CloseCultivationCycle;
using HortaComunitaria.Api.Features.Cultivation.CompleteDryingLot;
using HortaComunitaria.Api.Features.Cultivation.CreateDryingLot;
using HortaComunitaria.Api.Features.Cultivation.CorrectHarvest;
using HortaComunitaria.Api.Features.Cultivation.OpenCultivationCycle;
using HortaComunitaria.Api.Features.Cultivation.RegisterAgronomicObservation;
using HortaComunitaria.Api.Features.Cultivation.RegisterEstablishmentAssessment;
using HortaComunitaria.Api.Features.Cultivation.RegisterHarvest;
using HortaComunitaria.Api.Features.Cultivation.RegisterPlanting;
using HortaComunitaria.Api.Features.Cultivation.RegisterPropagationLot;
using HortaComunitaria.Api.Features.Gardens.AssignBedToSector;
using HortaComunitaria.Api.Features.Gardens.ConfigureBed;
using HortaComunitaria.Api.Features.Gardens.CreateBed;
using HortaComunitaria.Api.Features.Gardens.CreateGarden;
using HortaComunitaria.Api.Features.Gardens.CreateIrrigationSector;
using HortaComunitaria.Api.Features.Gardens.SaveLayout;
using HortaComunitaria.Api.Features.Environment.CreateEnvironmentalVariable;
using HortaComunitaria.Api.Features.Environment.CreateSamplingPoint;
using HortaComunitaria.Api.Features.Environment.RegisterEnvironmentalMeasurement;
using HortaComunitaria.Api.Features.Irrigation.RegisterIrrigation;
using HortaComunitaria.Api.Features.Occurrences.ChangeOccurrenceStatus;
using HortaComunitaria.Api.Features.Occurrences.OpenOccurrence;
using HortaComunitaria.Api.Features.Occurrences.ResolveOccurrence;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Demonstration.SeedValidationScenario;

/// <summary>
/// Comando administrativo explícito <c>--seed-demo</c>: monta, com dados fictícios, o roteiro da reunião de validação
/// (canteiro e espécie, plantio, avaliação de sobrevivência, replantio, manejo, colheitas sucessivas, ocorrências,
/// irrigações manuais, mapa e ajuste de setor/canteiro). Funciona como um cliente administrativo: chama os handlers
/// reais, com as mesmas validações e o mesmo histórico da interface, autorados pelo primeiro Adm ativo. Não roda no
/// startup nem em migrations; exige <c>--bootstrap-admin</c> e <c>--seed-embrapa</c> antes. As datas são relativas ao
/// dia da execução (ciclo de 12 semanas até hoje). Se a horta demonstrativa já existir, nada é gravado.
/// </summary>
internal sealed class SeedValidationScenarioCommandHandler(
    AppDbContext db, IHttpContextAccessor http, TimeProvider clock,
    CreateGardenCommandHandler createGarden, CreateIrrigationSectorCommandHandler createSector, CreateBedCommandHandler createBed,
    AssignBedToSectorCommandHandler assignBed, ConfigureBedCommandHandler configureBed, SaveLayoutCommandHandler saveLayout,
    CreateParticipantCommandHandler createParticipant, CreateInstitutionCommandHandler createInstitution, CreateInputCommandHandler createInput,
    RegisterPropagationLotCommandHandler registerLot, OpenCultivationCycleCommandHandler openCycle, RegisterPlantingCommandHandler registerPlanting,
    RegisterEstablishmentAssessmentCommandHandler registerAssessment, RegisterAgronomicObservationCommandHandler registerObservation,
    RegisterHarvestCommandHandler registerHarvest, CorrectHarvestCommandHandler correctHarvest, CloseCultivationCycleCommandHandler closeCycle,
    RegisterActivityCommandHandler registerActivity, OpenOccurrenceCommandHandler openOccurrence,
    ChangeOccurrenceStatusCommandHandler changeOccurrence, ResolveOccurrenceCommandHandler resolveOccurrence,
    RegisterIrrigationCommandHandler registerIrrigation, RegisterFinancialEntryCommandHandler registerCost,
    CreateDryingLotCommandHandler createDrying, CompleteDryingLotCommandHandler completeDrying,
    CreateEnvironmentalVariableCommandHandler createVariable, CreateSamplingPointCommandHandler createPoint,
    RegisterEnvironmentalMeasurementCommandHandler registerMeasurement,
    CorrectActivityCommandHandler correctActivity, InvalidateActivityCommandHandler invalidateActivity, InactivateParticipantCommandHandler inactivateParticipant,
    CorrectFinancialEntryCommandHandler correctCost, InvalidateFinancialEntryCommandHandler invalidateCost,
    CreateCultivarCommandHandler createCultivar, ConfigureCultivarVisualGrowthCommandHandler configureCultivarVisual,
    CorrectPlantingCommandHandler correctPlanting, CorrectEstablishmentAssessmentCommandHandler correctAssessment,
    CorrectAgronomicObservationCommandHandler correctObservation, InvalidateAgronomicObservationCommandHandler invalidateObservation,
    InvalidateHarvestCommandHandler invalidateHarvest, InactivatePropagationLotCommandHandler inactivateLot,
    InvalidateEnvironmentalMeasurementCommandHandler invalidateMeasurement, CorrectIrrigationCommandHandler correctIrrigation,
    InvalidateIrrigationCommandHandler invalidateIrrigation, CorrectOccurrenceCommandHandler correctOccurrence,
    UploadEvidenceCommandHandler uploadEvidence, ILogger<SeedValidationScenarioCommandHandler> logger)
{
    /// <summary>Código estável da horta demonstrativa; sua existência torna o comando idempotente.</summary>
    internal const string GardenCode = "DEMO-VALIDACAO";

    private const string Fictitious = "Registro fictício para a reunião de validação.";
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(-3);
    private static readonly string[] SpeciesKeys = ["alface", "tomate", "cenoura", "pimenta-annuum", "mostarda"];

    /// <summary>Retorna 0 em sucesso ou quando já aplicado e 1 quando faltam pré-requisitos.</summary>
    internal async Task<int> RunAsync(CancellationToken token)
    {
        using var process = new ProcessLog(logger, "Demonstration.SeedValidationScenario");
        process.Step("CheckingPrerequisites", "Conferindo administrador, catálogo Embrapa e horta demonstrativa");
        if (await db.Gardens.AnyAsync(x => x.Code == GardenCode, token))
        {
            process.Event("DemoScenarioAlreadyApplied", "Skipped", "Horta demonstrativa {GardenCode} já existe; nada foi gravado", GardenCode);
            return 0;
        }
        var actor = await db.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDemonstration && x.Profile == UserProfile.Administrator)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(token);
        if (actor is null) return Reject(process, "Nenhum administrador ativo; execute --bootstrap-admin antes.");
        var keys = SpeciesKeys.Select(x => "embrapa-2023:" + x).ToArray();
        var species = await db.Species.AsNoTracking().Where(x => x.CatalogKey != null && keys.Contains(x.CatalogKey) && x.IsActive)
            .ToDictionaryAsync(x => x.CatalogKey!["embrapa-2023:".Length..], x => x.Id, token);
        if (species.Count != SpeciesKeys.Length) return Reject(process, "Catálogo Embrapa ausente; execute --seed-embrapa antes.");
        var cultivars = await db.Cultivars.AsNoTracking().Where(x => species.Values.Contains(x.SpeciesId) && x.IsActive)
            .Select(x => new { x.SpeciesId, x.Name, x.Id }).ToListAsync(token);
        Guid? Cultivar(string key, string name) => cultivars.FirstOrDefault(x => x.SpeciesId == species[key] && x.Name == name)?.Id;

        // Os handlers resolvem a autoria pela identidade da sessão; aqui ela é o primeiro Adm ativo, como no --seed-embrapa.
        http.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.Value.ToString())], "seed-demo"))
        };
        var today = LocalPeriod.LocalDate(clock.GetUtcNow());
        var start = today.AddDays(-84);
        DateOnly Day(int offset) => start.AddDays(offset);
        DateTimeOffset At(int offset, int hour, int minute = 0) => new(Day(offset).ToDateTime(new TimeOnly(hour, minute)), LocalOffset);

        process.Step("CreatingStructure", "Criando horta, setores, canteiros e vigências fictícios");
        var garden = Ok(await createGarden.HandleAsync(new(GardenCode, "Horta demonstrativa (dados fictícios)",
            "Área fictícia usada na reunião de validação; não representa a horta real da FCT/UFG.", 400m, 240m, 40m), token)).Id;
        var north = Ok(await createSector.HandleAsync(garden, new("DEMO-S1", "Setor Norte (fictício)", "Folhosas e hortaliças de ciclo curto."), token));
        var south = Ok(await createSector.HandleAsync(garden, new("DEMO-S2", "Setor Sul (fictício)", "Hortaliças de fruto."), token));
        var experimental = Ok(await createSector.HandleAsync(garden, new("DEMO-S3", "Setor Experimental (fictício)", "Ensaios de semeadura direta."), token));
        var beds = new Dictionary<string, Guid>();
        foreach (var (code, sector, length, width) in new[]
        {
            ("C01", north, 5m, 1.2m), ("C02", north, 5m, 1.2m), ("C03", south, 5m, 1.2m),
            ("C04", south, 5m, 1.2m), ("C05", experimental, 3m, 1m), ("C06", experimental, 3m, 1m)
        })
        {
            var bed = Ok(await createBed.HandleAsync(garden, new("DEMO-" + code, $"Canteiro {code} (fictício)", null), token));
            var assigned = Ok(await assignBed.HandleAsync(garden, bed.Id, new(sector.Id, start, bed.Version, "Configuração inicial fictícia"), token));
            Ok(await configureBed.HandleAsync(garden, bed.Id, new(length * width, length, width, start, assigned.BedVersion, "Configuração inicial fictícia"), token));
            beds[code] = bed.Id;
        }

        process.Step("CreatingCatalogs", "Criando participantes, instituição, insumos e lotes de material fictícios");
        var ana = Ok(await createParticipant.HandleAsync(garden, new("Ana (participante fictícia)", "Estudante"), token)).Id;
        var bruno = Ok(await createParticipant.HandleAsync(garden, new("Bruno (participante fictício)", "Voluntário"), token)).Id;
        var carla = Ok(await createParticipant.HandleAsync(garden, new("Carla (participante fictícia)", "Técnica"), token)).Id;
        var diego = Ok(await createParticipant.HandleAsync(garden, new("Diego (colaborador fictício)", "Estudante"), token)).Id;
        var elisa = Ok(await createParticipant.HandleAsync(garden, new("Elisa (colaboradora fictícia)", "Professora"), token)).Id;
        var fabio = Ok(await createParticipant.HandleAsync(garden, new("Fábio (colaborador fictício)", "Morador do entorno"), token)).Id;
        // Instituições e insumos são cadastros globais: reaproveita os de uma execução anterior em vez de duplicar.
        var school = await db.Institutions.AsNoTracking().Where(x => x.Name == "Escola Estadual Exemplo (fictícia)").Select(x => (Guid?)x.Id).FirstOrDefaultAsync(token)
            ?? Ok(await createInstitution.HandleAsync(new("Escola Estadual Exemplo (fictícia)", "EEE", "Escola"), token)).Id;
        async Task<Guid> Input(string name, string category, string unit) =>
            await db.Inputs.AsNoTracking().Where(x => x.Name == name).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(token)
            ?? Ok(await createInput.HandleAsync(new(name, category, unit), token)).Id;
        var compost = await Input("Composto orgânico (demonstração)", "Adubo", "kg");
        var neem = await Input("Calda de neem (demonstração)", "Controle de pragas", "L");
        Guid Lot(PropagationLotResponse lot) => lot.Id;
        var lettuceLot = Lot(Ok(await registerLot.HandleAsync(garden, new("DEMO-L01", species["alface"], Cultivar("alface", "BRS Leila"), MaterialType.Seedling, "Viveiro fictício", Day(0), Fictitious), token)));
        var tomatoLot = Lot(Ok(await registerLot.HandleAsync(garden, new("DEMO-L02", species["tomate"], Cultivar("tomate", "BRS Sena"), MaterialType.Seedling, "Viveiro fictício", Day(5), Fictitious), token)));
        var carrotLot = Lot(Ok(await registerLot.HandleAsync(garden, new("DEMO-L03", species["cenoura"], Cultivar("cenoura", "Brasília"), MaterialType.Seed, "Doação fictícia", Day(9), Fictitious), token)));
        var pepperLot = Lot(Ok(await registerLot.HandleAsync(garden, new("DEMO-L04", species["pimenta-annuum"], null, MaterialType.Seedling, "Viveiro fictício", Day(20), Fictitious), token)));
        var mustardLot = Lot(Ok(await registerLot.HandleAsync(garden, new("DEMO-L05", species["mostarda"], Cultivar("mostarda", "Gisilba"), MaterialType.Seedling, "Produção própria fictícia", Day(30), Fictitious), token)));

        process.Step("RegisteringCultivation", "Registrando ciclos, plantios, replantios, avaliações, observações e colheitas");
        async Task<CultivationCycleResponse> Cycle(string bed, string key, Guid? cultivar, int day, decimal area, Guid? responsible) =>
            Ok(await openCycle.HandleAsync(garden, new(Guid.CreateVersion7(), beds[bed], species[key], cultivar, Day(day), area, responsible), token)).Detail;
        async Task<Guid> Plant(Guid cycle, Guid? lot, Guid? replaced, int day, int quantity, decimal? spacing, decimal? rows, Guid? responsible) =>
            Ok(await registerPlanting.HandleAsync(garden, new(Guid.CreateVersion7(), cycle, lot, replaced, Day(day),
                replaced is null ? PlantingKind.Initial : PlantingKind.Replacement, quantity, spacing, rows, responsible), token)).Detail.Id;
        async Task Assess(Guid planting, int day, int surviving, string? notes, Guid? responsible) =>
            Ok(await registerAssessment.HandleAsync(garden, new(Guid.CreateVersion7(), planting, Day(day), surviving, "Contagem de mudas vivas", notes, responsible), token));
        async Task Observe(Guid cycle, int day, string category, string description, decimal? measurement, string? unit, string? method, Guid? responsible) =>
            Ok(await registerObservation.HandleAsync(garden, new(Guid.CreateVersion7(), cycle, Day(day), category, description, measurement, unit, method, responsible), token));
        async Task<HarvestResponse> Harvest(Guid cycle, int day, decimal fresh, decimal? usable, string? loss, Guid? responsible) =>
            Ok(await registerHarvest.HandleAsync(garden, new(Guid.CreateVersion7(), cycle, Day(day), fresh, usable, loss, responsible), token)).Detail;

        // C01: alface com replantio, duas colheitas, correção da pesagem e encerramento; novo ciclo em seguida.
        var lettuce = await Cycle("C01", "alface", Cultivar("alface", "BRS Leila"), 0, 5m, ana);
        var lettucePlanting = await Plant(lettuce.Id, lettuceLot, null, 0, 60, 25m, 25m, ana);
        await Assess(lettucePlanting, 10, 52, "Oito mudas perdidas após chuva forte (fictício).", ana);
        var lettuceReplant = await Plant(lettuce.Id, lettuceLot, lettucePlanting, 12, 8, 25m, 25m, bruno);
        await Assess(lettuceReplant, 20, 7, null, bruno);
        await Observe(lettuce.Id, 30, "Crescimento das plantas", "Plantas uniformes, folhas bem formadas.", 18m, "cm", "Régua na planta mais alta", ana);
        var firstLettuce = await Harvest(lettuce.Id, 45, 6.4m, 5.9m, "Folhas externas danificadas", ana);
        await Harvest(lettuce.Id, 52, 3.2m, 3.0m, null, bruno);
        Ok(await correctHarvest.HandleAsync(garden, firstLettuce.Id, new(lettuce.Id, Day(45), 6.1m, 5.7m, "Folhas externas danificadas", ana,
            firstLettuce.Version, "Pesagem revisada na balança da horta (fictício)."), token));
        Ok(await closeCycle.HandleAsync(garden, lettuce.Id, new(Day(55), lettuce.Version, "Colheita do canteiro concluída (fictício)."), token));
        var lettuce2 = await Cycle("C01", "alface", Cultivar("alface", "BRS Leila"), 58, 5m, ana);
        var lettuce2Planting = await Plant(lettuce2.Id, lettuceLot, null, 58, 60, 25m, 25m, ana);
        await Assess(lettuce2Planting, 68, 56, null, ana);
        await Observe(lettuce2.Id, 80, "Crescimento das plantas", "Segundo ciclo em desenvolvimento.", 12m, "cm", "Régua na planta mais alta", ana);

        // C03: tomate com replantio, praga acompanhada por ocorrência e colheitas sucessivas.
        var tomato = await Cycle("C03", "tomate", Cultivar("tomate", "BRS Sena"), 5, 4.5m, carla);
        var tomatoPlanting = await Plant(tomato.Id, tomatoLot, null, 5, 20, 50m, 100m, carla);
        await Assess(tomatoPlanting, 15, 17, "Três mudas murchas (fictício).", carla);
        var tomatoReplant = await Plant(tomato.Id, tomatoLot, tomatoPlanting, 17, 3, 50m, 100m, carla);
        await Assess(tomatoReplant, 27, 3, null, carla);
        await Observe(tomato.Id, 40, "Crescimento das plantas", "Plantas tutoradas, início da floração.", 85m, "cm", "Trena", carla);
        await Observe(tomato.Id, 60, "Pragas, doenças ou deficiências", "Suspeita de mosca-branca nas folhas baixeiras, sem diagnóstico confirmado.", null, null, "Inspeção visual", carla);
        await Harvest(tomato.Id, 70, 2.4m, 2.2m, "Frutos rachados", carla);
        await Harvest(tomato.Id, 76, 3.1m, 3.0m, null, carla);
        await Harvest(tomato.Id, 82, 2.8m, 2.8m, null, bruno);

        // C05: cenoura por semeadura direta (sementes não entram no pegamento de mudas).
        var carrot = await Cycle("C05", "cenoura", Cultivar("cenoura", "Brasília"), 10, 3m, bruno);
        await Plant(carrot.Id, carrotLot, null, 10, 300, 5m, 20m, bruno);
        await Observe(carrot.Id, 25, "Germinação", "Emergência uniforme na maior parte do canteiro.", 80m, "%", "Estimativa visual", bruno);
        await Harvest(carrot.Id, 82, 4.1m, 3.6m, "Raízes bifurcadas", bruno);

        // C02: pimentão ainda sem colheita.
        var pepper = await Cycle("C02", "pimenta-annuum", null, 20, 4m, ana);
        var pepperPlanting = await Plant(pepper.Id, pepperLot, null, 20, 12, 50m, 80m, ana);
        await Assess(pepperPlanting, 30, 11, null, ana);
        await Observe(pepper.Id, 70, "Florescimento", "Primeiras flores abertas.", null, null, "Inspeção visual", ana);

        // C04: mostarda com colheitas sucessivas por corte e rebrote.
        var mustard = await Cycle("C04", "mostarda", Cultivar("mostarda", "Gisilba"), 30, 5m, bruno);
        var mustardPlanting = await Plant(mustard.Id, mustardLot, null, 30, 40, 30m, 30m, bruno);
        await Assess(mustardPlanting, 40, 38, null, bruno);
        var mustard1 = await Harvest(mustard.Id, 60, 2.0m, 1.8m, "Folhas amareladas", bruno);
        var mustard2 = await Harvest(mustard.Id, 70, 1.6m, 1.5m, null, bruno);
        await Observe(mustard.Id, 72, "Capacidade de rebrote", "Rebrote vigoroso após o segundo corte.", null, null, "Inspeção visual", bruno);
        var mustard3 = await Harvest(mustard.Id, 80, 1.4m, 1.4m, null, ana);

        process.Step("RegisteringIrrigations", "Registrando irrigações manuais, antes da integração com sensores");
        Guid? interrupted = null;
        foreach (var day in new[] { 50, 54, 58, 62, 66, 70, 74, 78, 82 })
        {
            await Irrigate(north.Id, ["C01", "C02"], day, 80m, VolumeSource.Estimated, "Regador de 10 L × 8 cargas");
            var stopped = day == 66;
            var south66 = await Irrigate(south.Id, ["C03", "C04"], day, stopped ? null : 90m, stopped ? null : VolumeSource.Estimated,
                stopped ? null : "Regador de 10 L × 9 cargas", stopped);
            if (stopped) interrupted = south66;
        }
        var lastExperimental = Guid.Empty;
        foreach (var day in new[] { 56, 64, 72, 80 })
            lastExperimental = await Irrigate(experimental.Id, ["C05"], day, 35.5m, VolumeSource.Measured, "Hidrômetro da torneira do setor");

        process.Step("RegisteringOccurrences", "Registrando ocorrências e atividades de manejo");
        var pest = Ok(await openOccurrence.HandleAsync(garden, new(Guid.CreateVersion7(), At(60, 8), "Praga",
            "Suspeita de mosca-branca no tomate do canteiro C03 (fictício).", carla, null, null, tomato.Id, null), token)).Detail;
        var pestFollowUp = Ok(await changeOccurrence.HandleAsync(garden, pest.Id, new(OccurrenceStatus.InProgress, pest.Version, "Monitoramento iniciado (fictício)."), token));
        Ok(await resolveOccurrence.HandleAsync(garden, pest.Id, new(At(66, 17), "Aplicação de calda de neem e monitoramento semanal sem novos focos (fictício).",
            pestFollowUp.Version, "Praga controlada (fictício)."), token));
        var leak = Ok(await openOccurrence.HandleAsync(garden, new(Guid.CreateVersion7(), At(66, 7, 40), "Infraestrutura",
            "Vazamento na mangueira do setor Sul interrompeu a irrigação (fictício).", bruno, null, null, null, interrupted), token)).Detail;

        async Task<ActivityResponse> Activity(string title, string category, int day, string? description, Guid? bed, Guid? sector, Guid? cycle, Guid? occurrence,
            (Guid Id, decimal Hours, string? Role, bool Audience)[] people, (Guid Id, decimal Quantity, string Unit)[]? inputs = null,
            bool operational = true, bool social = false, bool educational = false, string? objective = null, string? modality = null,
            int? audience = null, bool? complete = null, ActivityInstitutionRequest[]? institutions = null) =>
            Ok(await registerActivity.HandleAsync(garden, new(Guid.CreateVersion7(), title, category, Day(day), description, bed, sector, cycle, occurrence,
                operational, social, educational, objective, modality, audience, complete,
                [.. people.Select(x => new ActivityParticipantRequest(x.Id, x.Role, x.Hours, x.Audience))],
                [.. (inputs ?? []).Select(x => new InputUsageRequest(x.Id, x.Quantity, x.Unit))], institutions ?? []), token)).Detail;

        await Activity("Preparo do canteiro C01", "Preparo do solo", 0, Fictitious, beds["C01"], null, null, null,
            [(ana, 2m, "Execução", false), (bruno, 2m, "Execução", false)], [(compost, 20m, "kg")]);
        var weeding = await Activity("Capina dos canteiros do setor Norte", "Capina", 22, Fictitious, null, north.Id, null, null,
            [(ana, 1.5m, "Execução", false), (carla, 1.5m, "Execução", false)]);
        await Activity("Adubação de cobertura do tomate", "Adubação", 25, Fictitious, null, null, tomato.Id, null,
            [(bruno, 1m, "Execução", false)], [(compost, 8m, "kg")]);
        await Activity("Tutoramento do tomate", "Tutoramento", 35, Fictitious, null, null, tomato.Id, null,
            [(ana, 2m, "Execução", false), (bruno, 2m, "Execução", false)]);
        await Activity("Mutirão de manutenção da horta", "Manutenção", 50, "Limpeza dos corredores e reposição de cobertura morta (fictício).",
            null, null, null, null, [(ana, 3m, "Execução", false), (bruno, 3m, "Execução", false), (carla, 3m, "Coordenação", false)], social: true);
        await Activity("Aplicação de calda de neem no tomate", "Controle de pragas", 62, Fictitious, null, null, tomato.Id, pest.Id,
            [(carla, 1m, "Execução", false)], [(neem, 2m, "L")]);
        var workshop = await Activity("Oficina de horta com estudantes", "Oficina", 65, "Visita guiada aos canteiros e plantio de mudas (fictício).",
            null, null, null, null, [(carla, 2m, "Mediação", false)], operational: false, social: true, educational: true,
            objective: "Apresentar o ciclo de cultivo e o registro de dados em campo.", modality: "Presencial", audience: 25, complete: false,
            institutions: [new(school, "Escola participante")]);
        await Activity("Reposição de cobertura morta no setor Experimental", "Manutenção", 74, Fictitious, null, experimental.Id, null, null,
            [(bruno, 1m, "Execução", false)]);

        await Activity("Plantio coletivo com colaboradores", "Plantio", 58, "Novo ciclo de alface com a equipe de colaboradores (fictício).",
            null, null, lettuce2.Id, null, [(diego, 2m, "Execução", false), (elisa, 2m, "Orientação", false), (fabio, 2m, "Execução", false)], social: true);
        await Activity("Colheita e pesagem da mostarda", "Colheita", 80, Fictitious, null, null, mustard.Id, null,
            [(diego, 1m, "Execução", false), (fabio, 1m, "Pesagem", false)]);

        process.Step("RegisteringDrying", "Registrando secagem concluída e secagem em andamento da mostarda");
        var drying = Ok(await createDrying.HandleAsync(garden, new(Guid.CreateVersion7(), "DEMO-SEC01", "Secador solar", Day(71), elisa,
            [new(mustard1.Id, 1.8m), new(mustard2.Id, 1.5m)]), token)).Detail;
        Ok(await completeDrying.HandleAsync(garden, drying.Id, new(Day(78), 0.42m, drying.Version, "Pesagem após secagem (fictício)."), token));
        Ok(await createDrying.HandleAsync(garden, new(Guid.CreateVersion7(), "DEMO-SEC02", "Secador solar", Day(81), diego,
            [new(mustard3.Id, 1.0m)]), token));

        process.Step("RegisteringCosts", "Registrando custos previstos e realizados");
        async Task<FinancialEntryResponse> Cost(int day, string description, string category, decimal amount, FinancialNature nature, Guid? bed = null, Guid? cycle = null) =>
            Ok(await registerCost.HandleAsync(garden, new(Guid.CreateVersion7(), Day(day), description + " (fictício)", category, amount, nature, bed, cycle), token)).Detail;
        await Cost(0, "Mudas de alface e tomate", "Mudas e sementes", 120m, FinancialNature.Planned);
        await Cost(2, "Mudas de alface e tomate", "Mudas e sementes", 98.50m, FinancialNature.Actual);
        await Cost(9, "Sementes de cenoura", "Mudas e sementes", 18.90m, FinancialNature.Actual, null, carrot.Id);
        await Cost(0, "Composto orgânico", "Insumos", 60m, FinancialNature.Actual);
        await Cost(30, "Estacas e fitilho para tutoramento", "Materiais", 35m, FinancialNature.Actual, null, tomato.Id);
        await Cost(15, "Regadores e mangueira", "Ferramentas", 150m, FinancialNature.Planned);
        var tools = await Cost(18, "Regadores e mangueira", "Ferramentas", 162.40m, FinancialNature.Actual);
        await Cost(67, "Reparo do vazamento da mangueira", "Manutenção da irrigação", 45m, FinancialNature.Actual);
        await Cost(60, "Materiais da oficina com estudantes", "Atividades educativas", 80m, FinancialNature.Planned);

        process.Step("RegisteringEnvironment", "Registrando pontos de coleta e medições manuais de umidade do solo e temperatura");
        async Task<Guid> Variable(string code, string name, string quantity, string unit, bool soil) =>
            await db.EnvironmentalVariables.AsNoTracking().Where(x => x.Code == code).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(token)
            ?? Ok(await createVariable.HandleAsync(new(code, name, quantity, unit, "Variável usada no cenário demonstrativo.", soil), token)).Id;
        var moisture = await Variable("DEMO-UMIDADE-SOLO", "Umidade do solo (demonstração)", "Umidade volumétrica", "%", true);
        var temperature = await Variable("DEMO-TEMP-AR", "Temperatura do ar (demonstração)", "Temperatura", "°C", false);
        var points = new[]
        {
            Ok(await createPoint.HandleAsync(garden, new("DEMO-P1", "Ponto central do setor Norte (fictício)", "Entre C01 e C02", null, north.Id), token)).Id,
            Ok(await createPoint.HandleAsync(garden, new("DEMO-P2", "Ponto do canteiro de tomate (fictício)", "Cabeceira do C03", beds["C03"], null), token)).Id,
            Ok(await createPoint.HandleAsync(garden, new("DEMO-P3", "Ponto do setor Experimental (fictício)", "Ao lado do C05", beds["C05"], null), token)).Id,
        };
        var reading = 0;
        foreach (var day in new[] { 49, 53, 57, 61, 65, 69, 73, 77, 81, 83 })
        {
            for (var i = 0; i < points.Length; i++)
            {
                var value = 18m + (reading * 7 + i * 5) % 14;
                var questionable = day == 65 && i == 1;
                Ok(await registerMeasurement.HandleAsync(garden, new(Guid.CreateVersion7(), points[i], moisture, At(day, 8, 10 + i * 5), value, "%",
                    "Medidor portátil (leitura manual)", questionable ? MeasurementQuality.Questionable : MeasurementQuality.Normal, i == 0 ? diego : elisa,
                    questionable ? "Leitura logo após irrigação; revisar (fictício)." : null), token));
                reading++;
            }
            Ok(await registerMeasurement.HandleAsync(garden, new(Guid.CreateVersion7(), points[0], temperature, At(day, 12), 26m + day % 6, "°C",
                "Termômetro de bulbo", MeasurementQuality.Normal, diego, null), token));
        }

        process.Step("RegisteringLocalCultivar", "Cadastrando cultivar da horta, lote próprio e novo ciclo no canteiro experimental");
        const string LocalCultivarName = "Crespa da horta (fictícia)";
        // Cultivar é cadastro global: reaproveita a de uma execução anterior.
        var localCultivarId = await db.Cultivars.AsNoTracking().Where(x => x.SpeciesId == species["alface"] && x.Name == LocalCultivarName)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(token);
        if (localCultivarId is null)
        {
            var created = Ok(await createCultivar.HandleAsync(species["alface"], new(LocalCultivarName, "Seleção local de alface crespa mantida pela horta (dado fictício)."), token));
            Ok(await configureCultivarVisual.HandleAsync(species["alface"], created.Id, new(18, 40, created.Version, "Prazos observados na horta (fictício)."), token));
            localCultivarId = created.Id;
        }
        var localLot = Lot(Ok(await registerLot.HandleAsync(garden, new("DEMO-L06", species["alface"], localCultivarId, MaterialType.Seedling,
            "Mudas produzidas na horta com sementes próprias (fictício)", Day(70), Fictitious), token)));
        var localCycle = await Cycle("C06", "alface", localCultivarId, 76, 3m, diego);
        var localPlanting = await Plant(localCycle.Id, localLot, null, 76, 24, 25m, 25m, diego);
        await Assess(localPlanting, 83, 22, null, diego);

        process.Step("RegisteringCorrections", "Registrando correções, invalidações e inativações, sempre com motivo e histórico");
        // Irrigação: leitura do hidrômetro revisada e um lançamento indevido invalidado.
        var lastIrrigation = await db.Irrigations.AsNoTracking().SingleAsync(x => x.Id == lastExperimental, token);
        Ok(await correctIrrigation.HandleAsync(garden, lastExperimental, new(experimental.Id, [beds["C05"]], At(80, 7), At(80, 7, 25), 20m, 38.2m, null,
            VolumeSource.Measured, "Hidrômetro da torneira do setor", IrrigationStatus.Completed, null, "Registro manual fictício (sem sensores).",
            lastIrrigation.Version, "Leitura do hidrômetro revisada (fictício)."), token));
        var wrongIrrigation = await Irrigate(experimental.Id, ["C05"], 82, 30m, VolumeSource.Estimated, "Regador de 10 L × 3 cargas");
        var wrongIrrigationVersion = await db.Irrigations.AsNoTracking().Where(x => x.Id == wrongIrrigation).Select(x => x.Version).SingleAsync(token);
        Ok(await invalidateIrrigation.HandleAsync(garden, wrongIrrigation, new(wrongIrrigationVersion, "Lançada por engano: o setor não foi irrigado neste dia (fictício)."), token));

        // Custos: valor corrigido pela nota fiscal e lançamento em duplicidade invalidado.
        Ok(await correctCost.HandleAsync(garden, tools.Id, new(tools.EntryDate, tools.Description, tools.Category, 158.90m, tools.Nature, null, null,
            tools.Version, "Valor corrigido conforme a nota fiscal (fictício)."), token));
        var duplicateCost = await Cost(0, "Composto orgânico", "Insumos", 60m, FinancialNature.Actual);
        Ok(await invalidateCost.HandleAsync(garden, duplicateCost.Id, new(duplicateCost.Version, "Lançamento em duplicidade (fictício)."), token));

        // Cultivo: recontagens, observação corrigida, observação no ciclo errado e pesagem duplicada.
        var replantVersion = await db.Plantings.AsNoTracking().Where(x => x.Id == tomatoReplant).Select(x => x.Version).SingleAsync(token);
        Ok(await correctPlanting.HandleAsync(garden, tomatoReplant, new(tomato.Id, tomatoLot, tomatoPlanting, Day(17), PlantingKind.Replacement, 4, 50m, 100m, carla,
            replantVersion, "Contagem revisada: foram repostas quatro mudas (fictício)."), token));
        var lettuceAssessment = await db.EstablishmentAssessments.AsNoTracking().SingleAsync(x => x.PlantingId == lettucePlanting, token);
        Ok(await correctAssessment.HandleAsync(garden, lettuceAssessment.Id, new(lettucePlanting, Day(10), 51, "Contagem de mudas vivas",
            "Recontagem encontrou uma muda a menos (fictício).", ana, lettuceAssessment.Version, "Recontagem no canteiro (fictício)."), token));
        var germination = await db.AgronomicObservations.AsNoTracking().SingleAsync(x => x.CycleId == carrot.Id && x.Category == "Germinação", token);
        Ok(await correctObservation.HandleAsync(garden, germination.Id, new(carrot.Id, Day(25), "Germinação", "Emergência uniforme na maior parte do canteiro.",
            85m, "%", "Contagem por amostragem", bruno, germination.Version, "Estimativa refeita por amostragem (fictício)."), token));
        await Observe(pepper.Id, 75, "Pragas, doenças ou deficiências", "Folhas com manchas amareladas.", null, null, "Inspeção visual", carla);
        var misplacedOn = Day(75);
        var misplaced = await db.AgronomicObservations.AsNoTracking().SingleAsync(x => x.CycleId == pepper.Id && x.ObservedOn == misplacedOn, token);
        Ok(await invalidateObservation.HandleAsync(garden, misplaced.Id, new(misplaced.Version, "Registrada no ciclo errado; a observação era do tomate (fictício)."), token));
        var duplicateHarvest = await Harvest(tomato.Id, 82, 2.8m, 2.8m, null, bruno);
        Ok(await invalidateHarvest.HandleAsync(garden, duplicateHarvest.Id, new(duplicateHarvest.Version, "Pesagem lançada em duplicidade (fictício)."), token));

        // Atividades e ocorrência: horas corrigidas, atividade duplicada e descrição detalhada após inspeção.
        Ok(await correctActivity.HandleAsync(garden, weeding.Id, new(weeding.Title, weeding.Category, weeding.PerformedOn, weeding.Description, null, north.Id, null, null,
            true, false, false, null, null, null, null, [new(ana, "Execução", 1.5m, false), new(carla, "Execução", 2m, false)], [], [],
            weeding.Version, "Horas de Carla corrigidas para 2 h (fictício)."), token));
        var duplicateActivity = await Activity("Reposição de cobertura morta no setor Experimental", "Manutenção", 74, Fictitious, null, experimental.Id, null, null,
            [(bruno, 1m, "Execução", false)]);
        Ok(await invalidateActivity.HandleAsync(garden, duplicateActivity.Id, new(duplicateActivity.Version, "Atividade lançada duas vezes (fictício)."), token));
        Ok(await correctOccurrence.HandleAsync(garden, leak.Id, new(At(66, 7, 40), "Infraestrutura",
            "Vazamento na conexão da mangueira do setor Sul interrompeu a irrigação; aguardando peça de reposição (fictício).", leak.Status, null, null, bruno,
            null, null, null, interrupted, leak.Version, "Descrição detalhada após inspeção (fictício)."), token));

        // Ambiente: leitura duvidosa descartada.
        var doubtful = await db.EnvironmentalMeasurements.AsNoTracking().SingleAsync(x => x.GardenId == garden && x.Quality == MeasurementQuality.Questionable, token);
        Ok(await invalidateMeasurement.HandleAsync(garden, doubtful.Id, new(doubtful.Version, "Leitura descartada: feita logo após a irrigação (fictício)."), token));

        process.Step("RegisteringPhotos", "Anexando ilustrações fictícias aos registros");
        async Task Photo(EvidenceTarget target, Guid id, int day, string title, DemoIllustration.Theme theme)
        {
            var bytes = DemoIllustration.Png(theme);
            Ok(await uploadEvidence.HandleAsync(garden, new(Guid.CreateVersion7().ToString(), target.ToString(), id.ToString(), "Ilustração: " + title,
                "Imagem gerada pelo cenário demonstrativo; não é uma fotografia real.", Day(day).ToString("yyyy-MM-dd")),
                new UploadedFile("ilustracao.png", bytes.Length, () => new MemoryStream(bytes)), token));
        }
        await Photo(EvidenceTarget.Bed, beds["C01"], 0, "canteiro C01 preparado", new((70, 140, 60), null, 3, 11));
        await Photo(EvidenceTarget.Planting, lettuce2Planting, 58, "plantio do segundo ciclo de alface", new((96, 170, 70), null, 4, 12));
        await Photo(EvidenceTarget.Harvest, firstLettuce.Id, 45, "colheita da alface", new((110, 185, 80), null, 2, 13));
        await Photo(EvidenceTarget.CultivationCycle, tomato.Id, 40, "tomate tutorado em floração", new((55, 120, 50), (214, 60, 45), 3, 14));
        await Photo(EvidenceTarget.Occurrence, pest.Id, 60, "folhas do tomate com mosca-branca", new((150, 160, 70), (235, 235, 225), 2, 15));
        await Photo(EvidenceTarget.Activity, workshop.Id, 65, "oficina com estudantes", new((80, 150, 60), (240, 190, 60), 3, 16));
        await Photo(EvidenceTarget.DryingLot, drying.Id, 75, "folhas de mostarda no secador", new((60, 110, 45), null, 5, 17));

        // Encerramentos de cadastro ao fim do período: lote descartado sem uso e colaborador que deixou o projeto.
        var expiredLot = Ok(await registerLot.HandleAsync(garden, new("DEMO-L07", species["mostarda"], Cultivar("mostarda", "Gisilba"), MaterialType.Seed,
            "Doação fictícia", Day(40), Fictitious), token));
        Ok(await inactivateLot.HandleAsync(garden, expiredLot.Id, new(expiredLot.Version, "Sementes vencidas antes do plantio; lote descartado (fictício)."), token));
        var fabioVersion = await db.Participants.AsNoTracking().Where(x => x.Id == fabio).Select(x => x.Version).SingleAsync(token);
        Ok(await inactivateParticipant.HandleAsync(garden, fabio, new(fabioVersion, "Colaborador encerrou a participação no projeto (fictício)."), token));

        // Ajustes de hoje: a configuração física ainda está em definição. Não reclassificam os fatos anteriores.
        process.Step("AdjustingStructure", "Mudando um canteiro de setor e redimensionando outro a partir de hoje");
        var c04 = await db.Beds.AsNoTracking().SingleAsync(x => x.Id == beds["C04"], token);
        Ok(await assignBed.HandleAsync(garden, c04.Id, new(north.Id, today, c04.Version, "Reorganização fictícia: C04 passa ao setor Norte."), token));
        var c06 = await db.Beds.AsNoTracking().SingleAsync(x => x.Id == beds["C06"], token);
        Ok(await configureBed.HandleAsync(garden, c06.Id, new(4.2m, 3.5m, 1.2m, today, c06.Version, "Redimensionamento fictício do canteiro C06."), token));

        process.Step("SavingLayout", "Salvando o mapa da horta demonstrativa");
        LayoutElement Sector(string id, IrrigationSectorResponse sector, double x, double y, double w, double h) =>
            new(id, sector.Name, "sector", "rectangle", sector.Id, X: x, Y: y, Width: w, Height: h, RotationDeg: 0);
        LayoutElement Bed(string code, string sector, double x, double y, double w, double h) =>
            new(code, $"Canteiro {code} (fictício)", "bed", "rectangle", beds[code], SectorId: sector, X: x, Y: y, Width: w, Height: h, RotationDeg: 0);
        Ok(await saveLayout.HandleAsync(garden, new(Guid.CreateVersion7(), 0, new LayoutDocument(30, 20,
            [new(0, 0), new(30, 0), new(30, 20), new(0, 20)],
            [
                Sector("s1", north, 1, 1, 18, 8), Sector("s2", south, 1, 11, 18, 8), Sector("s3", experimental, 21, 1, 8, 18),
                Bed("C01", "s1", 2, 2, 5, 1.2), Bed("C02", "s1", 2, 5, 5, 1.2), Bed("C04", "s1", 9, 2, 5, 1.2),
                Bed("C03", "s2", 2, 13, 5, 1.2), Bed("C05", "s3", 22, 3, 3, 1), Bed("C06", "s3", 22, 8, 3.5, 1.2),
                new("ref-water", "Caixa d'água (referência fictícia)", "reference", "circle", CenterX: 25, CenterY: 16, Radius: 1)
            ])), token));

        process.Event("DemoScenarioApplied", "Created", "Horta demonstrativa {GardenId} criada com dados fictícios até {Today}", garden, today);
        process.Complete(Result<Unit>.Success(new()));
        return 0;

        async Task<Guid> Irrigate(Guid sector, string[] bedCodes, int day, decimal? volume, VolumeSource? source, string? method, bool stopped = false) =>
            Ok(await registerIrrigation.HandleAsync(garden, new(Guid.CreateVersion7(), sector, [.. bedCodes.Select(x => beds[x])], At(day, 7), At(day, 7, 25),
                stopped ? 8m : 20m, volume, null, source, method, stopped ? IrrigationStatus.Interrupted : IrrigationStatus.Completed, null,
                stopped ? "Registro manual fictício: irrigação interrompida por vazamento." : "Registro manual fictício (sem sensores)."), token)).Detail.Id;
    }

    private static T Ok<T>(Result<T> result) => result.IsSuccess ? result.Value
        : throw new InvalidOperationException($"Cenário demonstrativo rejeitado: {result.Error.Code} — {result.Error.Message}");

    private static int Reject(ProcessLog process, string message)
    {
        process.Event("DemoScenarioRejected", "Rejected", "Cenário demonstrativo não criado: {Reason}", message);
        return 1;
    }
}
