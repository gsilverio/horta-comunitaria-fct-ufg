using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class ActivityParticipantMapping : IEntityTypeConfiguration<ActivityParticipant>
{
    public void Configure(EntityTypeBuilder<ActivityParticipant> builder)
    {
        builder.ToTable("activity_participants", t => t.HasCheckConstraint("ck_activity_participants_value", "\"WorkHours\" IS NULL OR \"WorkHours\" >= 0"));
        builder.HasKey(x => new { x.ActivityId, x.ParticipantId });

        builder.HasOne<Activity>().WithMany(x => x.Participants).HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Role).HasMaxLength(120); builder.Property(x => x.WorkHours).HasPrecision(14, 3);
    }
}
