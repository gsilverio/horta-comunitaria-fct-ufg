using System.Security.Claims;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Features.Activities.Shared;
using HortaComunitaria.Api.Features.Cultivation.Shared;
using HortaComunitaria.Api.Features.Gardens.Shared;
using HortaComunitaria.Api.Features.Occurrences.Shared;
using HortaComunitaria.Api.Features.Activities.CreateInput;
using HortaComunitaria.Api.Features.Activities.CreateInstitution;
using HortaComunitaria.Api.Features.Activities.CreateParticipant;
using HortaComunitaria.Api.Features.Activities.RegisterActivity;
using HortaComunitaria.Api.Features.Cultivation.CloseCultivationCycle;
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
    RegisterIrrigationCommandHandler registerIrrigation, ILogger<SeedValidationScenarioCommandHandler> logger)
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
        var school = Ok(await createInstitution.HandleAsync(new("Escola Estadual Exemplo (fictícia)", "EEE", "Escola"), token)).Id;
        var compost = Ok(await createInput.HandleAsync(new("Composto orgânico (demonstração)", "Adubo", "kg"), token)).Id;
        var neem = Ok(await createInput.HandleAsync(new("Calda de neem (demonstração)", "Controle de pragas", "L"), token)).Id;
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
        await Harvest(mustard.Id, 60, 2.0m, 1.8m, "Folhas amareladas", bruno);
        await Harvest(mustard.Id, 70, 1.6m, 1.5m, null, bruno);
        await Observe(mustard.Id, 72, "Capacidade de rebrote", "Rebrote vigoroso após o segundo corte.", null, null, "Inspeção visual", bruno);
        await Harvest(mustard.Id, 80, 1.4m, 1.4m, null, ana);

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
        foreach (var day in new[] { 56, 64, 72, 80 })
            await Irrigate(experimental.Id, ["C05"], day, 35.5m, VolumeSource.Measured, "Hidrômetro da torneira do setor");

        process.Step("RegisteringOccurrences", "Registrando ocorrências e atividades de manejo");
        var pest = Ok(await openOccurrence.HandleAsync(garden, new(Guid.CreateVersion7(), At(60, 8), "Praga",
            "Suspeita de mosca-branca no tomate do canteiro C03 (fictício).", carla, null, null, tomato.Id, null), token)).Detail;
        var pestFollowUp = Ok(await changeOccurrence.HandleAsync(garden, pest.Id, new(OccurrenceStatus.InProgress, pest.Version, "Monitoramento iniciado (fictício)."), token));
        Ok(await resolveOccurrence.HandleAsync(garden, pest.Id, new(At(66, 17), "Aplicação de calda de neem e monitoramento semanal sem novos focos (fictício).",
            pestFollowUp.Version, "Praga controlada (fictício)."), token));
        Ok(await openOccurrence.HandleAsync(garden, new(Guid.CreateVersion7(), At(66, 7, 40), "Infraestrutura",
            "Vazamento na mangueira do setor Sul interrompeu a irrigação (fictício).", bruno, null, null, null, interrupted), token));

        async Task Activity(string title, string category, int day, string? description, Guid? bed, Guid? sector, Guid? cycle, Guid? occurrence,
            (Guid Id, decimal Hours, string? Role, bool Audience)[] people, (Guid Id, decimal Quantity, string Unit)[]? inputs = null,
            bool operational = true, bool social = false, bool educational = false, string? objective = null, string? modality = null,
            int? audience = null, bool? complete = null, ActivityInstitutionRequest[]? institutions = null) =>
            Ok(await registerActivity.HandleAsync(garden, new(Guid.CreateVersion7(), title, category, Day(day), description, bed, sector, cycle, occurrence,
                operational, social, educational, objective, modality, audience, complete,
                [.. people.Select(x => new ActivityParticipantRequest(x.Id, x.Role, x.Hours, x.Audience))],
                [.. (inputs ?? []).Select(x => new InputUsageRequest(x.Id, x.Quantity, x.Unit))], institutions ?? []), token));

        await Activity("Preparo do canteiro C01", "Preparo do solo", 0, Fictitious, beds["C01"], null, null, null,
            [(ana, 2m, "Execução", false), (bruno, 2m, "Execução", false)], [(compost, 20m, "kg")]);
        await Activity("Capina dos canteiros do setor Norte", "Capina", 22, Fictitious, null, north.Id, null, null,
            [(ana, 1.5m, "Execução", false), (carla, 1.5m, "Execução", false)]);
        await Activity("Adubação de cobertura do tomate", "Adubação", 25, Fictitious, null, null, tomato.Id, null,
            [(bruno, 1m, "Execução", false)], [(compost, 8m, "kg")]);
        await Activity("Tutoramento do tomate", "Tutoramento", 35, Fictitious, null, null, tomato.Id, null,
            [(ana, 2m, "Execução", false), (bruno, 2m, "Execução", false)]);
        await Activity("Mutirão de manutenção da horta", "Manutenção", 50, "Limpeza dos corredores e reposição de cobertura morta (fictício).",
            null, null, null, null, [(ana, 3m, "Execução", false), (bruno, 3m, "Execução", false), (carla, 3m, "Coordenação", false)], social: true);
        await Activity("Aplicação de calda de neem no tomate", "Controle de pragas", 62, Fictitious, null, null, tomato.Id, pest.Id,
            [(carla, 1m, "Execução", false)], [(neem, 2m, "L")]);
        await Activity("Oficina de horta com estudantes", "Oficina", 65, "Visita guiada aos canteiros e plantio de mudas (fictício).",
            null, null, null, null, [(carla, 2m, "Mediação", false)], operational: false, social: true, educational: true,
            objective: "Apresentar o ciclo de cultivo e o registro de dados em campo.", modality: "Presencial", audience: 25, complete: false,
            institutions: [new(school, "Escola participante")]);
        await Activity("Reposição de cobertura morta no setor Experimental", "Manutenção", 74, Fictitious, null, experimental.Id, null, null,
            [(bruno, 1m, "Execução", false)]);

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
