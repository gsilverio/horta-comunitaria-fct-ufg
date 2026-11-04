using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class SamplingPointMapping : IEntityTypeConfiguration<SamplingPoint>
{
    public void Configure(EntityTypeBuilder<SamplingPoint> builder)
    {
        builder.ToTable("sampling_points", t => t.HasCheckConstraint("ck_sampling_point_context", "num_nonnulls(\"BedId\", \"SectorId\") <= 1"));
        builder.ToTable("sampling_points", table =>
        {
            table.HasCheckConstraint("ck_sampling_points_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(1000);

        builder.HasAlternateKey(x => new { x.GardenId, x.Id }); builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<IrrigationSector>().WithMany().HasForeignKey(x => new { x.GardenId, x.SectorId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasIndex(x => new { x.GardenId, x.Code }).IsUnique();
    }
}
