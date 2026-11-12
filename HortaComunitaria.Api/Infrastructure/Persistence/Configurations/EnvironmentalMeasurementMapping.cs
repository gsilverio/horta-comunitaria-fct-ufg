using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class EnvironmentalMeasurementMapping : IEntityTypeConfiguration<EnvironmentalMeasurement>
{
    public void Configure(EntityTypeBuilder<EnvironmentalMeasurement> builder)
    {
        builder.ToTable("environmental_measurements");
        builder.ToTable("environmental_measurements", table =>
        {
            table.HasCheckConstraint("ck_environmental_measurements_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_environmental_measurements_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.CollectedAt, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.AppUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Value).HasPrecision(14, 3); builder.Property(x => x.Unit).HasMaxLength(40).IsRequired(); builder.Property(x => x.Method).HasMaxLength(200).IsRequired(); builder.Property(x => x.Quality).HasColumnType("measurement_quality"); builder.Property(x => x.Notes).HasMaxLength(1000); builder.HasOne<SamplingPoint>().WithMany().HasForeignKey(x => new { x.GardenId, x.SamplingPointId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<EnvironmentalVariable>().WithMany().HasForeignKey(x => x.VariableId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<IrrigationSector>().WithMany().HasForeignKey(x => new { x.GardenId, x.SectorId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<BedSectorAssignment>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId, x.SectorId, x.BedSectorAssignmentId }).HasPrincipalKey(x => new { x.GardenId, x.BedId, x.IrrigationSectorId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
