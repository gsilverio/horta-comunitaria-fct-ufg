using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class InputUsageMapping : IEntityTypeConfiguration<InputUsage>
{
    public void Configure(EntityTypeBuilder<InputUsage> builder)
    {
        builder.ToTable("input_usages", t => t.HasCheckConstraint("ck_input_usages_value", "\"Quantity\" > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<Activity>().WithMany(x => x.InputUsages).HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgriculturalInput>().WithMany().HasForeignKey(x => x.InputId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Quantity).HasPrecision(14, 3); builder.Property(x => x.Unit).HasMaxLength(40).IsRequired();
    }
}
