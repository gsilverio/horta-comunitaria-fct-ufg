using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class AgriculturalInputMapping : IEntityTypeConfiguration<AgriculturalInput>
{
    public void Configure(EntityTypeBuilder<AgriculturalInput> builder)
    {
        builder.ToTable("agricultural_inputs");
        builder.ToTable("agricultural_inputs", table =>
        {
            table.HasCheckConstraint("ck_agricultural_inputs_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(120);
        builder.Property(x => x.ReferenceUnit).HasMaxLength(40).IsRequired();

    }
}
