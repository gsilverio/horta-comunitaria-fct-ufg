using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Costs;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

internal sealed class FinancialEntryMapping : IEntityTypeConfiguration<FinancialEntry>
{
    public void Configure(EntityTypeBuilder<FinancialEntry> builder)
    {
        builder.ToTable("financial_entries", t => t.HasCheckConstraint("ck_financial_entries_values", "\"AmountBrl\" >= 0 AND num_nonnulls(\"BedId\", \"CycleId\") <= 1"));
        builder.ToTable("financial_entries", table =>
        {
            table.HasCheckConstraint("ck_financial_entries_version", "\"Version\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GardenId, x.Id });
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Validity).HasColumnType("record_validity");
        builder.Property(x => x.CreationPayload).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.GardenId, x.OperationId }).IsUnique().HasDatabaseName("ux_financial_entries_garden_operation");
        builder.HasIndex(x => new { x.GardenId, x.EntryDate, x.Id }).IsDescending(false, true, true);
        builder.HasOne<Garden>().WithMany().HasForeignKey(x => x.GardenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Identity.AppUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired(); builder.Property(x => x.Category).HasMaxLength(120).IsRequired(); builder.Property(x => x.AmountBrl).HasPrecision(14, 2); builder.Property(x => x.Nature).HasColumnType("financial_nature"); builder.HasOne<Bed>().WithMany().HasForeignKey(x => new { x.GardenId, x.BedId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<CultivationCycle>().WithMany().HasForeignKey(x => new { x.GardenId, x.CycleId }).HasPrincipalKey(x => new { x.GardenId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
