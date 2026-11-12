using System.Net.Mail;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>
/// Comando administrativo explícito <c>--bootstrap-admin</c>: cria o primeiro Adm a partir da seção <c>Bootstrap</c>
/// (variáveis <c>Bootstrap__Email</c>, <c>Bootstrap__DisplayName</c>, <c>Bootstrap__Password</c>). Não roda no startup
/// nem em migrations, recusa-se quando já existe Adm ativo e exige troca da senha no primeiro acesso.
/// </summary>
internal sealed class AdministratorBootstrap(AppDbContext db, UserManager<AppUser> users, IOptions<BootstrapAdministratorOptions> options,
    TimeProvider clock, ILogger<AdministratorBootstrap> logger)
{
    /// <summary>Retorna 0 em sucesso e 1 em rejeição, com o motivo registrado no log.</summary>
    internal async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Identity.BootstrapAdministrator");
        var email = options.Value.Email.Trim();
        var name = options.Value.DisplayName.Trim();
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || email.Length > AppUser.EmailMaxLength ||
            name.Length is 0 or > AppUser.DisplayNameMaxLength || string.IsNullOrEmpty(options.Value.Password) ||
            options.Value.Password.Length > PasswordPolicy.MaximumLength)
            return Reject("BootstrapInvalidInput", "Informe Bootstrap__Email, Bootstrap__DisplayName e Bootstrap__Password válidos.");

        process.Step("CheckingAdministrators", "Conferindo se já existe administrador ativo");
        if (await db.Users.AnyAsync(x => x.IsActive && x.Profile == UserProfile.Administrator, cancellationToken))
            return Reject("BootstrapAlreadyDone", "Já existe administrador ativo; crie os demais usuários pela tela Usuários.");

        process.Step("CreatingAdministrator", "Criando administrador inicial");
        var user = AppUser.Create(email, name, UserProfile.Administrator, clock.GetUtcNow());
        var created = await users.CreateAsync(user, options.Value.Password);
        if (!created.Succeeded)
            return Reject("BootstrapRejected", string.Join(" ", created.Errors.Select(x => x.Description).Distinct()));
        process.Event("AdministratorCreated", "Created", "Administrador {UserId} criado; troca de senha exigida no primeiro acesso", user.Id);
        return 0;

        int Reject(string eventName, string message)
        {
            process.Event(eventName, "Rejected", "Administrador inicial não criado: {Reason}", message);
            return 1;
        }
    }
}
