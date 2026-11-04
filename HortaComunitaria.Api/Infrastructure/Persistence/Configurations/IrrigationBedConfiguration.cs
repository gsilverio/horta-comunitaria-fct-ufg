using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Irrigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class IrrigationBedConfiguration : IEntityTypeConfiguration<IrrigationBed>
{
    public void Configure(EntityTypeBuilder<IrrigationBed> builder)
    {
        builder.ToTable("irrigation_beds");
        builder.HasKey(x => new { x.IrrigationId, x.BedId });
        builder.HasOne<BedSectorAssignment>().WithMany()
            .HasForeignKey(x => new { x.GardenId, x.BedId, x.IrrigationSectorId, x.BedSectorAssignmentId })
            .HasPrincipalKey(x => new { x.GardenId, x.BedId, x.IrrigationSectorId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
