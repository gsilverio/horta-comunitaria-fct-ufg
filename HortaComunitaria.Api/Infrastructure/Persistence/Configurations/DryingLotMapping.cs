using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class DryingLotMapping : IEntityTypeConfiguration<DryingLot>
{
    public void Configure(EntityTypeBuilder<DryingLot> builder)
    {
        builder.ToTable("drying_lots");
        builder.ToTable("drying_lots", table =>
        {
            table.HasCheckConstraint("ck_drying_lots_state", "((\"Status\" = 'completed' AND \"EndedOn\" IS NOT NULL AND \"EndedOn\" >= \"StartedOn\" AND \"TotalDryMassKg\" IS NOT NULL AND \"TotalDryMassKg\" >= 0) OR (\"Status\" = 'in_progress' AND \"EndedOn\" IS NULL AND \"TotalDryMassKg\" IS NULL))");
            table.HasCheckConstraint("ck_drying_lots_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => new { x.GardenId, x.DryingLotId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(x => x.Status).HasColumnType("drying_status");
        builder.HasIndex(x => new { x.GardenId, x.Code }).IsUnique();
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_drying_lots_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.StartedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.AppUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired(); builder.Property(x => x.Method).HasMaxLength(200); builder.Property(x => x.TotalDryMassKg).HasPrecision(14, 3); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
