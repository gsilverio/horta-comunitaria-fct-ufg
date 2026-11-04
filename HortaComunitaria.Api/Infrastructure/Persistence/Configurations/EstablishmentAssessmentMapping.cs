using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class EstablishmentAssessmentMapping : IEntityTypeConfiguration<EstablishmentAssessment>
{
    public void Configure(EntityTypeBuilder<EstablishmentAssessment> builder)
    {
        builder.ToTable("establishment_assessments", t => t.HasCheckConstraint("ck_establishment_assessments_values", "\"SurvivingQuantity\" >= 0"));
        builder.ToTable("establishment_assessments", table =>
        {
            table.HasCheckConstraint("ck_establishment_assessments_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_establishment_assessments_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.AssessedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Identity.User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Method).HasMaxLength(200).IsRequired(); builder.Property(x => x.Notes).HasMaxLength(1000); builder.HasOne<Planting>().WithMany().HasForeignKey(x => new { x.GardenId, x.PlantingId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
