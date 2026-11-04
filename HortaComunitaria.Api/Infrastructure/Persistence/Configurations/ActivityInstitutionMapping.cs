using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class ActivityInstitutionMapping : IEntityTypeConfiguration<ActivityInstitution>
{
    public void Configure(EntityTypeBuilder<ActivityInstitution> builder)
    {
        builder.ToTable("activity_institutions");
        builder.HasKey(x => new { x.ActivityId, x.InstitutionId });

        builder.HasOne<Activity>().WithMany(x => x.Institutions).HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Institution>().WithMany().HasForeignKey(x => x.InstitutionId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Role).HasMaxLength(120);
    }
}
