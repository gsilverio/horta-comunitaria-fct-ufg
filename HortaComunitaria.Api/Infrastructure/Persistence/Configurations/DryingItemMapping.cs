using HortaComunitaria.Api.Domain.Cultivation;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class DryingItemMapping : IEntityTypeConfiguration<DryingItem>
{
    public void Configure(EntityTypeBuilder<DryingItem> builder)
    {
        builder.ToTable("drying_items", t => t.HasCheckConstraint("ck_drying_items_value", "\"AllocatedMassKg\" > 0"));
        builder.HasKey(x => new { x.DryingLotId, x.HarvestId });

        builder.HasOne<DryingLot>().WithMany(x => x.Items).HasForeignKey(x => new { x.GardenId, x.DryingLotId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Harvest>().WithMany().HasForeignKey(x => new { x.GardenId, x.HarvestId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.AllocatedMassKg).HasPrecision(14, 3);
    }
}
