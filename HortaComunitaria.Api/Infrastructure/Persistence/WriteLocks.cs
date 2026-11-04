using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

/// <summary>Locks transacionais do MVP; leituras de contexto e gravações usam os mesmos recursos.</summary>
internal static class WriteLocks
{
    internal static async Task HarvestsAsync(AppDbContext db, Guid gardenId, IEnumerable<Guid> ids, CancellationToken token)
    {
        foreach (var id in ids.Distinct().Order())
            await db.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM harvests WHERE \"GardenId\" = {gardenId} AND \"Id\" = {id} FOR UPDATE").ToListAsync(token);
    }
    // Cadastros globais e seus dependentes novos compartilham este lock conservador no protótipo.
    internal static Task CatalogsAsync(AppDbContext db, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({731991})", cancellationToken);

    internal static async Task GardenAsync(AppDbContext db, Guid gardenId, CancellationToken cancellationToken, bool shared = false)
    {
        if (shared)
            await db.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM gardens WHERE \"Id\" = {gardenId} FOR SHARE").ToListAsync(cancellationToken);
        else
            await db.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM gardens WHERE \"Id\" = {gardenId} FOR UPDATE").ToListAsync(cancellationToken);
    }

    internal static async Task BedsAsync(AppDbContext db, Guid gardenId, IEnumerable<Guid> ids, CancellationToken cancellationToken, bool shared = false)
    {
        foreach (var id in ids.Distinct().Order())
            if (shared)
                await db.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM beds WHERE \"GardenId\" = {gardenId} AND \"Id\" = {id} FOR SHARE").ToListAsync(cancellationToken);
            else
                await db.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM beds WHERE \"GardenId\" = {gardenId} AND \"Id\" = {id} FOR UPDATE").ToListAsync(cancellationToken);
    }
}
