using System.Net.Mail;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>
/// Comando administrativo explícito <c>--seed-test-admins</c>: cria contas Adm de teste para o ambiente demonstrativo.
/// Contas e senha vêm só do ambiente (<c>TestAdmins__Accounts</c> no formato <c>email|Nome;email|Nome</c> e
/// <c>TestAdmins__Password</c>); nada fica versionado. Contas já existentes são mantidas sem alteração. Cada pessoa
/// troca a senha no primeiro acesso, como em qualquer conta nova.
/// </summary>
internal sealed class TestAdministratorsSeed(AppDbContext db, UserManager<AppUser> users, IConfiguration configuration, TimeProvider clock,
    ILogger<TestAdministratorsSeed> logger)
{
    /// <summary>Retorna 0 quando todas as contas existem ao final e 1 em entrada inválida ou rejeição.</summary>
    internal async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Identity.SeedTestAdministrators");
        var password = configuration["TestAdmins:Password"];
        var accounts = (configuration["TestAdmins:Accounts"] ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Split('|', 2, StringSplitOptions.TrimEntries)).ToArray();
        if (string.IsNullOrEmpty(password) || accounts.Length == 0 || accounts.Any(x => x.Length != 2 || x[1].Length is 0 or > AppUser.DisplayNameMaxLength ||
            !MailAddress.TryCreate(x[0], out var parsed) || parsed.Address != x[0] || x[0].Length > AppUser.EmailMaxLength))
            return Reject(process, "Informe TestAdmins__Accounts (email|Nome;...) e TestAdmins__Password válidos.");

        var failures = 0;
        foreach (var account in accounts)
        {
            if (await db.Users.AnyAsync(x => x.NormalizedEmail == account[0].ToUpperInvariant(), cancellationToken))
            {
                process.Event("TestAdministratorSkipped", "Skipped", "Conta de teste {Email} já existe; mantida sem alteração", account[0]);
                continue;
            }
            var user = AppUser.Create(account[0], account[1], UserProfile.Administrator, clock.GetUtcNow());
            var created = await users.CreateAsync(user, password);
            if (created.Succeeded)
                process.Event("TestAdministratorCreated", "Created", "Conta Adm de teste {UserId} criada; troca de senha exigida no primeiro acesso", user.Id);
            else
            {
                failures++;
                process.Event("TestAdministratorRejected", "Rejected", "Conta de teste {Email} não criada: {Reason}", account[0],
                    string.Join(" ", created.Errors.Select(x => x.Description).Distinct()));
            }
        }
        return failures == 0 ? 0 : 1;
    }

    private static int Reject(ProcessLog process, string message)
    {
        process.Event("TestAdministratorsRejected", "Rejected", "Contas de teste não criadas: {Reason}", message);
        return 1;
    }
}
