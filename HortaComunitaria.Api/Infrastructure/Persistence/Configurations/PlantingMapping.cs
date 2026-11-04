using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Activities;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class PlantingMapping : IEntityTypeConfiguration<Planting>
{
    public void Configure(EntityTypeBuilder<Planting> builder)
    {
        builder.ToTable("plantings", t => t.HasCheckConstraint("ck_plantings_values", "\"PlantedQuantity\" > 0"));
        builder.ToTable("plantings", table =>
        {
            table.HasCheckConstraint("ck_plantings_spacing", "(\"PlantSpacingCm\" IS NULL OR \"PlantSpacingCm\" > 0) AND (\"RowSpacingCm\" IS NULL OR \"RowSpacingCm\" > 0)");
            table.HasCheckConstraint("ck_plantings_reference", "\"ReplacedPlantingId\" IS NULL OR \"ReplacedPlantingId\" <> \"Id\"");
            table.HasCheckConstraint("ck_plantings_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_plantings_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.PlantedOn, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Domain.Identity.User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Kind).HasColumnType("planting_kind"); builder.Property(x => x.PlantSpacingCm).HasPrecision(14, 3); builder.Property(x => x.RowSpacingCm).HasPrecision(14, 3); builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<PropagationLot>().WithMany().HasForeignKey(x => new { x.GardenId, x.PropagationLotId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Planting>().WithMany().HasForeignKey(x => new { x.GardenId, x.ReplacedPlantingId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
