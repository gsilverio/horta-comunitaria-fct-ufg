using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Features.Users.CreateUser;

/// <summary>Dados da nova conta.</summary>
/// <param name="Email">E-mail de acesso (único).</param>
/// <param name="DisplayName">Nome de exibição.</param>
/// <param name="Profile">Perfil de acesso.</param>
/// <param name="InitialPassword">Senha inicial entregue à pessoa; a troca é exigida no primeiro acesso.</param>
internal sealed record CreateUserRequest(string? Email, string? DisplayName, UserProfile? Profile, string? InitialPassword);

/// <summary>Cria uma conta ativa com senha inicial; inclusão e histórico são gravados juntos.</summary>
internal sealed class CreateUserCommandHandler(AppDbContext db, UserManager<AppUser> users, CurrentAuthor author, TimeProvider clock,
    ILogger<CreateUserCommandHandler> logger)
{
    public async Task<Result<UserResponse>> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Users.CreateUser");
        Result<UserResponse> Reject(Error error) => process.Complete(Result<UserResponse>.Failure(error));
        if ((UserProjection.ValidateEmail(request.Email) ?? UserProjection.ValidateName(request.DisplayName) ??
             UserProjection.ValidateProfile(request.Profile)) is { } invalid) return Reject(invalid);
        if (string.IsNullOrEmpty(request.InitialPassword) || request.InitialPassword.Length > PasswordPolicy.MaximumLength)
            return Reject(Error.Validation("initialPassword", $"Informe a senha inicial (até {PasswordPolicy.MaximumLength} caracteres)."));

        var actor = await author.ResolveAsync(cancellationToken);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        var user = AppUser.Create(request.Email!.Trim(), request.DisplayName!.Trim(), request.Profile!.Value, now);
        process.Step("Persisting", "Gravando conta {UserId} e histórico na mesma transação", user.Id);
        // O UserManager confirma a conta no mesmo SaveChanges do histórico já anexado ao contexto.
        db.AuditEntries.Add(AuditSnapshots.Create(null, "User", user.Id, actor, now, null, AuditSnapshots.Capture(UserProjection.Snapshot(user))));
        var created = await users.CreateAsync(user, request.InitialPassword);
        if (!created.Succeeded)
        {
            db.ChangeTracker.Clear();
            var duplicate = created.Errors.Any(x => x.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName));
            var message = string.Join(" ", created.Errors.Select(x => x.Description).Distinct());
            return Reject(duplicate ? Error.Conflict("user.duplicate_email", message) : Error.Validation("initialPassword", message));
        }
        process.Event("UserCreated", "Created", "Conta {UserId} criada com perfil {Profile}", user.Id, user.Profile);
        return process.Complete(Result<UserResponse>.Success(UserProjection.Map(user, now)));
    }
}
