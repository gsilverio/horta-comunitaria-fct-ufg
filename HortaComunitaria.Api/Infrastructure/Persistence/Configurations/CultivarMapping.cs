using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class CultivarMapping : IEntityTypeConfiguration<Cultivar>
{
    public void Configure(EntityTypeBuilder<Cultivar> builder)
    {
        builder.ToTable("cultivars");
        builder.ToTable("cultivars", table =>
        {
            table.HasCheckConstraint("ck_cultivars_version", "\"Version\" > 0");
        });
        builder.ToTable("cultivars", table => table.HasCheckConstraint("ck_cultivar_visual_timing",
            "(\"VisualGrowthDays\" IS NULL AND \"VisualHarvestDays\" IS NULL) OR (\"VisualGrowthDays\" IS NOT NULL AND \"VisualHarvestDays\" IS NOT NULL AND \"VisualGrowthDays\" > 0 AND \"VisualHarvestDays\" > \"VisualGrowthDays\")"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CatalogKey).HasMaxLength(80);
        builder.HasIndex(x => x.CatalogKey).IsUnique();
        builder.HasAlternateKey(x => new { x.SpeciesId, x.Id }); builder.HasOne<Species>().WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Restrict);
    }
}
