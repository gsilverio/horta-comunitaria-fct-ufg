using HortaComunitaria.Api.Domain.Gardens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class GardenConfiguration : IEntityTypeConfiguration<Garden>
{
    public void Configure(EntityTypeBuilder<Garden> builder)
    {
        builder.ToTable("gardens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(1);
        builder.Property(x => x.Location).HasMaxLength(1000);
        builder.Property(x => x.InstitutionalAreaM2).HasPrecision(14, 3);
        builder.Property(x => x.OperationalAreaM2).HasPrecision(14, 3);
        builder.Property(x => x.ExperimentalAreaM2).HasPrecision(14, 3);
        builder.Property(x => x.Id).HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

    }
}
