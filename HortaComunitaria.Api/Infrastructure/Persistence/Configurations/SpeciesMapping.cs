using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class SpeciesMapping : IEntityTypeConfiguration<Species>
{
    public void Configure(EntityTypeBuilder<Species> builder)
    {
        builder.ToTable("species");
        builder.ToTable("species", table =>
        {
            table.HasCheckConstraint("ck_species_version", "\"Version\" > 0");
        });
        builder.ToTable("species", table => table.HasCheckConstraint("ck_species_visual_timing",
            "(\"VisualGrowthDays\" IS NULL AND \"VisualHarvestDays\" IS NULL) OR (\"VisualGrowthDays\" IS NOT NULL AND \"VisualHarvestDays\" IS NOT NULL AND \"VisualGrowthDays\" > 0 AND \"VisualHarvestDays\" > \"VisualGrowthDays\")"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.CommonName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ScientificName).HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CatalogKey).HasMaxLength(80);
        builder.HasIndex(x => x.CatalogKey).IsUnique();
        builder.Property(x => x.ImageBase64).HasMaxLength(240000);
        builder.Property(x => x.ImageMetadataJson).HasColumnType("jsonb");
        builder.ToTable("species", table => table.HasCheckConstraint("ck_species_catalog_image",
            "(\"ImageBase64\" IS NULL AND \"ImageMetadataJson\" IS NULL) OR (\"ImageBase64\" IS NOT NULL AND \"ImageMetadataJson\" IS NOT NULL AND length(\"ImageBase64\") BETWEEN 4 AND 240000)"));

    }
}
