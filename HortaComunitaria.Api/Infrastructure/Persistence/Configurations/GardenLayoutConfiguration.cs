using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class GardenLayoutConfiguration : IEntityTypeConfiguration<GardenLayout>
{
    public void Configure(EntityTypeBuilder<GardenLayout> builder)
    {
        builder.ToTable("garden_layouts");
        builder.HasKey(x => x.GardenId);
        builder.Property(x => x.Document).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.SavedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
