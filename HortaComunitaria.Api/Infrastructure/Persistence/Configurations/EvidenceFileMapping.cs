using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Evidence;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

/// <summary>Metadados das fotografias; exatamente uma FK de destino por linha, sempre na mesma horta.</summary>
internal sealed class EvidenceFileMapping : IEntityTypeConfiguration<EvidenceFile>
{
    public void Configure(EntityTypeBuilder<EvidenceFile> builder)
    {
        builder.ToTable("evidence_files", table =>
        {
            table.HasCheckConstraint("ck_evidence_files_single_target", "num_nonnulls(\"BedId\", \"CycleId\", \"PlantingId\", \"EstablishmentAssessmentId\", \"AgronomicObservationId\", \"HarvestId\", \"DryingLotId\", \"ActivityId\", \"OccurrenceId\") = 1");
            table.HasCheckConstraint("ck_evidence_files_size", "\"SizeBytes\" > 0");
            table.HasCheckConstraint("ck_evidence_files_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Ignore(x => x.Target);
        builder.Ignore(x => x.TargetId);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.Title).HasMaxLength(EvidenceFile.TitleMaximum).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(EvidenceFile.DescriptionMaximum);
        builder.Property(x => x.StorageKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("ux_evidence_files_storage_key");
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_evidence_files_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.RecordedAt, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.AppUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Planting>().WithMany().HasForeignKey(x => new { x.GardenId, x.PlantingId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EstablishmentAssessment>().WithMany().HasForeignKey(x => new { x.GardenId, x.EstablishmentAssessmentId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgronomicObservation>().WithMany().HasForeignKey(x => new { x.GardenId, x.AgronomicObservationId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Harvest>().WithMany().HasForeignKey(x => new { x.GardenId, x.HarvestId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DryingLot>().WithMany().HasForeignKey(x => new { x.GardenId, x.DryingLotId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.GardenId, x.ActivityId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Occurrence>().WithMany().HasForeignKey(x => new { x.GardenId, x.OccurrenceId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
