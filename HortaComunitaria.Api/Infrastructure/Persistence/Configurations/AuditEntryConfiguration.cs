using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Infrastructure.Identity;
using IrrigationRecord = HortaComunitaria.Api.Domain.Irrigation.Irrigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Operation).HasColumnType("audit_operation").IsRequired();
        builder.Property(x => x.Before).HasColumnType("jsonb");
        builder.Property(x => x.After).HasColumnType("jsonb").IsRequired();
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        // FK opcional mantém a proteção do histórico de irrigação ao ampliar a auditoria a outros registros.
        builder.HasOne<IrrigationRecord>().WithMany().HasForeignKey(x => new { x.GardenId, x.IrrigationId })
            .HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.GardenId, x.EntityKey, x.OccurredAt, x.Id }).IsDescending(false, false, true, true);
    }
}
