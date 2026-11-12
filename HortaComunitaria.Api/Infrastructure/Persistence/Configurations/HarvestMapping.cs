using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class HarvestMapping : IEntityTypeConfiguration<Harvest>
{
    public void Configure(EntityTypeBuilder<Harvest> builder)
    {
        builder.ToTable("harvests", t => t.HasCheckConstraint("ck_harvests_values", "\"FreshMassKg\" >= 0 AND (\"UsableMassKg\" IS NULL OR (\"UsableMassKg\" >= 0 AND \"UsableMassKg\" <= \"FreshMassKg\"))"));
        builder.ToTable("harvests", table =>
        {
            table.HasCheckConstraint("ck_harvests_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_harvests_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.HarvestedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.AppUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.FreshMassKg).HasPrecision(14, 3); builder.Property(x => x.UsableMassKg).HasPrecision(14, 3); builder.Property(x => x.LossReason).HasMaxLength(1000); builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
