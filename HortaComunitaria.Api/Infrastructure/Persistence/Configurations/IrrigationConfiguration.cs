using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Identity;
using IrrigationRecord = HortaComunitaria.Api.Domain.Irrigation.Irrigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class IrrigationConfiguration : IEntityTypeConfiguration<IrrigationRecord>
{
    public void Configure(EntityTypeBuilder<IrrigationRecord> builder)
    {
        builder.ToTable("irrigations", table =>
        {
            table.HasCheckConstraint("ck_irrigations_period", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
            table.HasCheckConstraint("ck_irrigations_measures", "(\"VolumeLiters\" IS NULL OR \"VolumeLiters\" >= 0) AND " +
                "(\"ActiveDurationMinutes\" IS NULL OR \"ActiveDurationMinutes\" >= 0) AND " +
                "(\"FlowRateLitersPerMinute\" IS NULL OR \"FlowRateLitersPerMinute\" >= 0)");
            table.HasCheckConstraint("ck_irrigations_active_duration", "\"EndedAt\" IS NULL OR \"ActiveDurationMinutes\" IS NULL OR " +
                "\"ActiveDurationMinutes\" <= EXTRACT(EPOCH FROM (\"EndedAt\" - \"StartedAt\")) / 60");
            table.HasCheckConstraint("ck_irrigations_volume_origin", "(\"VolumeLiters\" IS NULL AND \"VolumeSource\" IS NULL AND \"VolumeMethod\" IS NULL) OR " +
                "(\"VolumeLiters\" IS NOT NULL AND \"VolumeSource\" IS NOT NULL AND \"VolumeMethod\" IS NOT NULL AND length(btrim(\"VolumeMethod\")) > 0)");
            table.HasCheckConstraint("ck_irrigations_version", "\"Version\" >= 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.EndedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.RecordedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.VolumeLiters).HasPrecision(14, 3);
        builder.Property(x => x.ActiveDurationMinutes).HasPrecision(14, 3);
        builder.Property(x => x.FlowRateLitersPerMinute).HasPrecision(14, 3);
        builder.Property(x => x.VolumeSource).HasColumnType("volume_source");
        builder.Property(x => x.Status).HasColumnType("irrigation_status").IsRequired();
        builder.Property(x => x.Validity).HasColumnType("record_validity").IsRequired();
        builder.Property(x => x.VolumeMethod).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Version).IsConcurrencyToken().IsRequired();
        builder.Property(x => x.OperationId).IsRequired().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired().Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_irrigations_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.StartedAt, x.Id }).IsDescending(false, true, true);
        builder.HasIndex(x => new { x.GardenId, x.IrrigationSectorId, x.StartedAt, x.Id }).IsDescending(false, false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<IrrigationSector>().WithMany().HasForeignKey(x => new { x.GardenId, x.IrrigationSectorId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Participant>().WithMany().HasForeignKey(x => new { x.GardenId, x.ResponsibleParticipantId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        // Setor é contexto do fato, não identidade: poderá ser corrigido na etapa 3.
        // Horta/irrigação têm chave estável; factory/handler conferem o mesmo setor dos filhos.
        builder.HasMany(x => x.Beds).WithOne().HasForeignKey(x => new { x.GardenId, x.IrrigationId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Beds).HasField("beds").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
