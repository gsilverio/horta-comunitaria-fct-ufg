using HortaComunitaria.Api.Domain.Environment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class EnvironmentalVariableMapping : IEntityTypeConfiguration<EnvironmentalVariable>
{
    public void Configure(EntityTypeBuilder<EnvironmentalVariable> builder)
    {
        builder.ToTable("environmental_variables");
        builder.ToTable("environmental_variables", table =>
        {
            table.HasCheckConstraint("ck_environmental_variables_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Quantity).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ReferenceUnit).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
