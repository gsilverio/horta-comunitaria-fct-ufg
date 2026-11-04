using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class ActivityMapping : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activities", table =>
        {
            table.HasCheckConstraint("ck_activities_context", "num_nonnulls(\"BedId\", \"SectorId\", \"CycleId\") <= 1");
            table.HasCheckConstraint("ck_activities_dimensions", "\"IsOperational\" OR \"IsSocial\" OR \"IsEducational\"");
            table.HasCheckConstraint("ck_activities_audience", "\"ReportedAudienceTotal\" IS NULL OR \"ReportedAudienceTotal\" >= 0");
            table.HasCheckConstraint("ck_activities_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Participants).WithOne().HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Participants).HasField("_participants").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.InputUsages).WithOne().HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.InputUsages).HasField("_inputUsages").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(x => x.Institutions).WithOne().HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Institutions).HasField("_institutions").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_activities_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.PerformedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Identity.User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Title).HasMaxLength(120).IsRequired(); builder.Property(x => x.Category).HasMaxLength(120).IsRequired(); builder.Property(x => x.Description).HasMaxLength(1000); builder.Property(x => x.Objective).HasMaxLength(1000); builder.Property(x => x.Modality).HasMaxLength(120); builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<IrrigationSector>().WithMany().HasForeignKey(x => new { x.GardenId, x.SectorId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Occurrence>().WithMany().HasForeignKey(x => new { x.GardenId, x.OccurrenceId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
