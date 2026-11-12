using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.IntegrationTests;

/// <summary>Dados explícitos de cada teste, exclusivamente no banco criado pela fixture.</summary>
internal static class TestContext
{
    /// <summary>Impede que a fixture grave fora de um banco <c>horta_test_</c> criado para o caso.</summary>
    internal static void EnsureDisposableDatabase(AppDbContext db)
    {
        if (!db.Database.GetDbConnection().Database.StartsWith("horta_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("O contexto de teste exige um banco descartável horta_test_.");
    }

    /// <summary>Horta, setores, canteiros e participante fictícios. Usuários são criados por <see cref="ApiFactory.InitializeUsersAsync"/>.</summary>
    internal static async Task InsertAsync(AppDbContext db)
    {
        EnsureDisposableDatabase(db);

        db.Gardens.Add(Garden.Create("DEMO", "Horta fictícia de teste", id: DemoIds.Garden).Value);
        var sector = IrrigationSector.Create(DemoIds.Garden, "S1", "Setor de teste 1", id: DemoIds.SectorOne).Value;
        db.IrrigationSectors.AddRange(sector,
            IrrigationSector.Create(DemoIds.Garden, "S2", "Setor de teste 2", id: DemoIds.SectorTwo).Value);
        for (var index = 0; index < DemoIds.Beds.Length; index++)
        {
            var bed = Bed.Create(DemoIds.Garden, $"B{index + 1}", $"Canteiro de teste {index + 1}", id: DemoIds.Beds[index]).Value;
            db.Beds.Add(bed);
            db.BedSectorAssignments.Add(BedSectorAssignment.Create(bed, sector,
                new DateOnly(2026, 10, 1), null, [], DemoIds.Assignments[index]).Value);
        }
        db.Participants.Add(Participant.Create(DemoIds.Garden, "Participante fictício", id: DemoIds.Participant).Value);
        await db.SaveChangesAsync();
    }
}
