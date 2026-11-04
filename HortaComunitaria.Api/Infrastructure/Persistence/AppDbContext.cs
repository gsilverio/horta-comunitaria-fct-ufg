using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.Costs;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Identity;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using IrrigationRecord = HortaComunitaria.Api.Domain.Irrigation.Irrigation;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

/// <summary>Unidade de persistência dos slices; não adiciona camada de repositories.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Hortas cadastradas.</summary>
    public DbSet<Garden> Gardens => Set<Garden>();
    /// <summary>Setores locais.</summary>
    public DbSet<IrrigationSector> IrrigationSectors => Set<IrrigationSector>();
    /// <summary>Canteiros com identidade estável.</summary>
    public DbSet<Bed> Beds => Set<Bed>();
    /// <summary>Contexto histórico dos canteiros.</summary>
    public DbSet<BedSectorAssignment> BedSectorAssignments => Set<BedSectorAssignment>();
    /// <summary>Dimensões e área efetiva por vigência.</summary>
    public DbSet<BedConfiguration> BedConfigurations => Set<BedConfiguration>();
    /// <summary>Responsáveis pelo trabalho.</summary>
    public DbSet<Participant> Participants => Set<Participant>();
    /// <summary>Autoria técnica; não constitui um sistema de autenticação.</summary>
    public DbSet<User> Users => Set<User>();
    /// <summary>Fatos manuais de irrigação.</summary>
    public DbSet<IrrigationRecord> Irrigations => Set<IrrigationRecord>();
    /// <summary>Canteiros atendidos com contexto histórico.</summary>
    public DbSet<IrrigationBed> IrrigationBeds => Set<IrrigationBed>();
    /// <summary>Histórico confirmado na mesma transação do fato.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    /// <summary>Registros Species do MVP.</summary>
    public DbSet<Species> Species => Set<Species>();
    /// <summary>Registros Cultivars do MVP.</summary>
    public DbSet<Cultivar> Cultivars => Set<Cultivar>();
    /// <summary>Registros PropagationLots do MVP.</summary>
    public DbSet<PropagationLot> PropagationLots => Set<PropagationLot>();
    /// <summary>Registros EnvironmentalVariables do MVP.</summary>
    public DbSet<EnvironmentalVariable> EnvironmentalVariables => Set<EnvironmentalVariable>();
    /// <summary>Registros SamplingPoints do MVP.</summary>
    public DbSet<SamplingPoint> SamplingPoints => Set<SamplingPoint>();
    /// <summary>Registros Inputs do MVP.</summary>
    public DbSet<AgriculturalInput> Inputs => Set<AgriculturalInput>();
    /// <summary>Registros Institutions do MVP.</summary>
    public DbSet<Institution> Institutions => Set<Institution>();
    /// <summary>Registros EnvironmentalMeasurements do MVP.</summary>
    public DbSet<EnvironmentalMeasurement> EnvironmentalMeasurements => Set<EnvironmentalMeasurement>();
    /// <summary>Registros CultivationCycles do MVP.</summary>
    public DbSet<CultivationCycle> CultivationCycles => Set<CultivationCycle>();
    /// <summary>Registros Plantings do MVP.</summary>
    public DbSet<Planting> Plantings => Set<Planting>();
    /// <summary>Registros EstablishmentAssessments do MVP.</summary>
    public DbSet<EstablishmentAssessment> EstablishmentAssessments => Set<EstablishmentAssessment>();
    /// <summary>Registros AgronomicObservations do MVP.</summary>
    public DbSet<AgronomicObservation> AgronomicObservations => Set<AgronomicObservation>();
    /// <summary>Registros Harvests do MVP.</summary>
    public DbSet<Harvest> Harvests => Set<Harvest>();
    /// <summary>Registros FinancialEntries do MVP.</summary>
    public DbSet<FinancialEntry> FinancialEntries => Set<FinancialEntry>();
    /// <summary>Registros Occurrences do MVP.</summary>
    public DbSet<Occurrence> Occurrences => Set<Occurrence>();
    /// <summary>Registros DryingItems do MVP.</summary>
    public DbSet<DryingItem> DryingItems => Set<DryingItem>();
    /// <summary>Registros ActivityParticipants do MVP.</summary>
    public DbSet<ActivityParticipant> ActivityParticipants => Set<ActivityParticipant>();
    /// <summary>Registros InputUsages do MVP.</summary>
    public DbSet<InputUsage> InputUsages => Set<InputUsage>();
    /// <summary>Registros ActivityInstitutions do MVP.</summary>
    public DbSet<ActivityInstitution> ActivityInstitutions => Set<ActivityInstitution>();
    /// <summary>Registros DryingLots do MVP.</summary>
    public DbSet<DryingLot> DryingLots => Set<DryingLot>();
    /// <summary>Registros Activities do MVP.</summary>
    public DbSet<Activity> Activities => Set<Activity>();
    /// <summary>Aplica mapeamentos separados do domínio.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
