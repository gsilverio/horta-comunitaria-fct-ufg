using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class BedSectorAssignmentConfiguration : IEntityTypeConfiguration<BedSectorAssignment>
{
    public void Configure(EntityTypeBuilder<BedSectorAssignment> builder)
    {
        builder.ToTable("bed_sector_assignments", table =>
            table.HasCheckConstraint("ck_bed_sector_assignments_period",
                "\"ValidUntil\" IS NULL OR \"ValidUntil\" > \"ValidFrom\""));
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.BedId, x.IrrigationSectorId, x.Id });
        builder.Property(x => x.Id).HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(x => x.GardenId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.BedId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.IrrigationSectorId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnType("date").IsRequired();
        builder.Property(x => x.ValidUntil).HasColumnType("date");
        builder.HasOne<Bed>().WithMany()
            .HasForeignKey(x => new { x.GardenId, x.BedId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IrrigationSector>().WithMany()
            .HasForeignKey(x => new { x.GardenId, x.IrrigationSectorId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BedId, x.ValidFrom }).IsUnique();
        builder.HasIndex(x => new { x.GardenId, x.IrrigationSectorId, x.ValidFrom });
    }
}
