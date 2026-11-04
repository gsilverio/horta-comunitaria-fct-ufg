using HortaComunitaria.Api.Domain.Activities;
using HortaComunitaria.Api.Domain.Gardens;
using HortaComunitaria.Api.Domain.Identity;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.IntegrationTests;

/// <summary>Dados explícitos de cada teste, exclusivamente no banco criado pela fixture.</summary>
internal static class TestContext
{
    internal static async Task InsertAsync(AppDbContext db)
    {
        if (!db.Database.GetDbConnection().Database.StartsWith("horta_test_", StringComparison.Ordinal))
            throw new InvalidOperationException("O contexto de teste exige um banco descartável horta_test_.");

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
        db.Users.Add(User.Create("demo:operator", "Operador de demonstração", id: DemoIds.Operator).Value);
        await db.SaveChangesAsync();
    }
}
