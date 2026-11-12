using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class AgronomicObservationMapping : IEntityTypeConfiguration<AgronomicObservation>
{
    public void Configure(EntityTypeBuilder<AgronomicObservation> builder)
    {
        builder.ToTable("agronomic_observations");
        builder.ToTable("agronomic_observations", table =>
        {
            table.HasCheckConstraint("ck_agronomic_observations_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_agronomic_observations_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.ObservedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.AppUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Category).HasMaxLength(120).IsRequired(); builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(); builder.Property(x => x.Measurement).HasPrecision(14, 3); builder.Property(x => x.Unit).HasMaxLength(40); builder.Property(x => x.Method).HasMaxLength(200); builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
