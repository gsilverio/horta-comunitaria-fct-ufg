using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("beds");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(1);
        builder.Property(x => x.Location).HasMaxLength(1000);
        builder.Property(x => x.Id).HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.Code }).IsUnique();
        builder.Property(x => x.GardenId).HasColumnType("uuid").IsRequired();
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
    }
}
