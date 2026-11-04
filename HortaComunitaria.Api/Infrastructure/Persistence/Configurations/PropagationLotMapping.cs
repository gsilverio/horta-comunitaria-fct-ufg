using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class PropagationLotMapping : IEntityTypeConfiguration<PropagationLot>
{
    public void Configure(EntityTypeBuilder<PropagationLot> builder)
    {
        builder.ToTable("propagation_lots");
        builder.ToTable("propagation_lots", table =>
        {
            table.HasCheckConstraint("ck_propagation_lots_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();

        builder.Property(x => x.MaterialType).HasColumnType("material_type");
        builder.Property(x => x.Origin).HasMaxLength(1000);

        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id }); builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Species>().WithMany().HasForeignKey(x => x.SpeciesId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Cultivar>().WithMany().HasForeignKey(x => new { x.SpeciesId, x.CultivarId }).HasPrincipalKey(x => new { x.SpeciesId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasIndex(x => new { x.GardenId, x.Code }).IsUnique();
    }
}
