using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HortaComunitaria.Api.IntegrationTests;

public sealed class PersistenceIntegrityTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Duplicate_codes_and_invalid_foreign_keys_are_blocked_by_postgres()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            db.Gardens.Add(Garden.Create("DEMO", "Duplicada").Value);
            var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(duplicate.InnerException).SqlState);
            db.ChangeTracker.Clear();
            db.IrrigationSectors.Add(IrrigationSector.Create(Guid.CreateVersion7(), "MISSING", "Sem horta").Value);
            var missing = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(missing.InnerException).SqlState);
        });
    }

    [Fact]
    public async Task Composite_foreign_keys_block_cross_garden_assignments_and_period_check_is_enforced()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            var other = Garden.Create("OTHER", "Outra").Value;
            var otherSector = IrrigationSector.Create(other.Id, "S1", "Setor de outra horta").Value;
            db.Gardens.Add(other);
            db.IrrigationSectors.Add(otherSector);
            var unassignedBed = Bed.Create(DemoIds.Garden, "FK_TEST", "Canteiro sem vigência").Value;
            db.Beds.Add(unassignedBed);
            await db.SaveChangesAsync();
            var exception = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"""
                INSERT INTO bed_sector_assignments ("Id", "GardenId", "BedId", "IrrigationSectorId", "ValidFrom")
                VALUES ({Guid.CreateVersion7()}, {DemoIds.Garden}, {unassignedBed.Id}, {otherSector.Id}, {new DateOnly(2026, 11, 1)})
                """));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
            var period = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync($"""
                INSERT INTO bed_sector_assignments ("Id", "GardenId", "BedId", "IrrigationSectorId", "ValidFrom", "ValidUntil")
                VALUES ({Guid.CreateVersion7()}, {DemoIds.Garden}, {unassignedBed.Id}, {DemoIds.SectorOne},
                    {new DateOnly(2026, 11, 1)}, {new DateOnly(2026, 11, 1)})
                """));
            Assert.Equal(PostgresErrorCodes.CheckViolation, period.SqlState);
        });
    }

    [Fact]
    public async Task Referenced_catalog_cannot_be_deleted_and_migration_is_applied()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
            var deletion = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlAsync(
                $"DELETE FROM gardens WHERE \"Id\" = {DemoIds.Garden}"));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, deletion.SqlState);
            Assert.Equal(1, await db.Gardens.CountAsync());
        });
    }

    [Fact]
    public async Task Factories_reject_overlapping_periods_and_cross_garden_context()
    {
        using var factory = await postgres.CreateFactoryAsync();
        using var client = factory.CreateClient();
        await factory.WithDbAsync(async db =>
        {
            var bed = await db.Beds.SingleAsync(x => x.Id == DemoIds.Beds[0]);
            var sector = await db.IrrigationSectors.SingleAsync(x => x.Id == DemoIds.SectorOne);
            var existing = await db.BedSectorAssignments.Where(x => x.BedId == bed.Id).ToListAsync();
            Assert.False(BedSectorAssignment.Create(bed, sector, new(2026, 10, 8), null, existing).IsSuccess);
            var other = IrrigationSector.Create(Guid.CreateVersion7(), "OTHER", "Outro setor").Value;
            Assert.False(BedSectorAssignment.Create(bed, other, new(2026, 9, 1), new(2026, 10, 1), []).IsSuccess);
            Assert.False(BedSectorAssignment.Create(bed, sector, new(2026, 10, 1), new(2026, 10, 1), []).IsSuccess);
        });
    }
}
