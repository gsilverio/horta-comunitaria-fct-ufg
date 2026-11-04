using HortaComunitaria.Api.Domain.Gardens;
using BedConfigurationRecord = HortaComunitaria.Api.Domain.Gardens.BedConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class BedConfigurationMapping : IEntityTypeConfiguration<BedConfigurationRecord>
{
    public void Configure(EntityTypeBuilder<BedConfigurationRecord> builder)
    {
        builder.ToTable("bed_configurations", table =>
        {
            table.HasCheckConstraint("ck_bed_configurations_period", "\"ValidUntil\" IS NULL OR \"ValidUntil\" > \"ValidFrom\"");
            table.HasCheckConstraint("ck_bed_configurations_area", "\"CultivableAreaM2\" > 0 AND (\"LengthMeters\" IS NULL OR \"LengthMeters\" > 0) AND (\"WidthMeters\" IS NULL OR \"WidthMeters\" > 0)");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.BedId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CultivableAreaM2).HasPrecision(14, 3);
        builder.Property(x => x.LengthMeters).HasPrecision(14, 3);
        builder.Property(x => x.WidthMeters).HasPrecision(14, 3);
        builder.HasIndex(x => new { x.BedId, x.ValidFrom }).IsUnique();
        builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
