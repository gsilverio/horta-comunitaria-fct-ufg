using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class CultivationCycleMapping : IEntityTypeConfiguration<CultivationCycle>
{
    public void Configure(EntityTypeBuilder<CultivationCycle> builder)
    {
        builder.ToTable("cultivation_cycles", t => t.HasCheckConstraint("ck_cultivation_cycles_values", "\"OccupiedAreaM2\" > 0 AND \"ReferenceAreaM2\" > 0 AND \"OccupiedAreaM2\" <= \"ReferenceAreaM2\" AND (\"EndedOn\" IS NULL OR \"EndedOn\" >= \"StartedOn\")"));
        builder.ToTable("cultivation_cycles", table =>
        {
            table.HasCheckConstraint("ck_cultivation_cycles_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_cultivation_cycles_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.StartedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Identity.User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.OccupiedAreaM2).HasPrecision(14, 3); builder.Property(x => x.ReferenceAreaM2).HasPrecision(14, 3); builder.Property(x => x.Status).HasColumnType("cultivation_status"); builder.HasOne<Domain.Gardens.BedConfiguration>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId, x.BedConfigurationId }).HasPrincipalKey(x => new { x.GardenId, x.BedId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Species>().WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Cultivar>().WithMany().HasForeignKey(x => new { x.SpeciesId, x.CultivarId }).HasPrincipalKey(x => new { x.SpeciesId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
