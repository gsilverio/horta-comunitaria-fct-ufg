using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class OccurrenceMapping : IEntityTypeConfiguration<Occurrence>
{
    public void Configure(EntityTypeBuilder<Occurrence> builder)
    {
        builder.ToTable("occurrences", t => t.HasCheckConstraint("ck_occurrences_values", "num_nonnulls(\"BedId\", \"SectorId\", \"CycleId\", \"IrrigationId\") <= 1 AND (\"ResolvedAt\" IS NULL OR \"ResolvedAt\" >= \"OpenedAt\")"));
        builder.ToTable("occurrences", table =>
        {
            table.HasCheckConstraint("ck_occurrences_state", "((\"Status\" = 'resolved' AND \"ResolvedAt\" IS NOT NULL AND \"ResolvedAt\" >= \"OpenedAt\" AND \"Resolution\" IS NOT NULL) OR (\"Status\" <> 'resolved' AND \"ResolvedAt\" IS NULL AND \"Resolution\" IS NULL))");
            table.HasCheckConstraint("ck_occurrences_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_occurrences_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.OpenedAt, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Identity.User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Category).HasMaxLength(120).IsRequired(); builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(); builder.Property(x => x.Status).HasColumnType("occurrence_status"); builder.Property(x => x.Resolution).HasMaxLength(1000); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<IrrigationSector>().WithMany().HasForeignKey(x => new { x.GardenId, x.SectorId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<HortaComunitaria.Api.Domain.Irrigation.Irrigation>().WithMany().HasForeignKey(x => new { x.GardenId, x.IrrigationId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
