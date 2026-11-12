using System.Text.Json.Serialization;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using HortaComunitaria.Api.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Users.UpdateUser;

/// <summary>Alteração de acesso feita pela administração.</summary>
/// <param name="DisplayName">Nome de exibição.</param>
/// <param name="Profile">Perfil de acesso.</param>
/// <param name="IsActive">Situação da conta.</param>
/// <param name="ExpectedVersion">Versão observada na consulta.</param>
/// <param name="Reason">Motivo registrado no histórico.</param>
internal sealed record UpdateUserRequest(string? DisplayName, UserProfile? Profile, [property: JsonRequired] bool IsActive,
    string? ExpectedVersion, string? Reason);

/// <summary>
/// Altera nome, perfil e situação. Mudança de perfil ou situação revoga as sessões da conta. O Adm não pode rebaixar
/// nem desativar a própria conta, o que mantém ao menos um administrador ativo.
/// </summary>
internal sealed class UpdateUserCommandHandler(AppDbContext db, UserManager<AppUser> users, CurrentAuthor author, TimeProvider clock,
    ILogger<UpdateUserCommandHandler> logger)
{
    public async Task<Result<UserResponse>> HandleAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Users.UpdateUser", new Dictionary<string, object?> { ["UserId"] = id });
        Result<UserResponse> Reject(Error error) => process.Complete(Result<UserResponse>.Failure(error));
        if ((UserProjection.ValidateName(request.DisplayName) ?? UserProjection.ValidateProfile(request.Profile)) is { } invalid) return Reject(invalid);
        if (string.IsNullOrWhiteSpace(request.ExpectedVersion)) return Reject(Error.Validation("expectedVersion", "Versão observada é obrigatória."));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            return Reject(Error.Validation("reason", "Informe o motivo da alteração (até 500 caracteres)."));

        var actor = await author.ResolveAsync(cancellationToken);
        process.Step("CheckingContext", "Conferindo conta e versão");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsDemonstration, cancellationToken);
        if (user is null) return Reject(new Error("resource.not_found", "Usuário não encontrado.", ErrorCategory.NotFound));
        if (user.ConcurrencyStamp != request.ExpectedVersion)
            return Reject(Error.Conflict("record.version_conflict", "O usuário foi alterado; recarregue e revise antes de salvar."));
        if (user.Id == actor && (!request.IsActive || request.Profile != UserProfile.Administrator))
            return Reject(Error.Conflict("user.self_demotion", "Você não pode desativar nem retirar o perfil Adm da própria conta."));

        var name = request.DisplayName!.Trim();
        var profile = request.Profile!.Value;
        if (user.DisplayName == name && user.Profile == profile && user.IsActive == request.IsActive)
            return process.Complete(Result<UserResponse>.Success(UserProjection.Map(user, clock.GetUtcNow())));
        var accessChanged = user.Profile != profile || user.IsActive != request.IsActive;
        var before = AuditSnapshots.Capture(UserProjection.Snapshot(user));
        user.UpdateAccess(name, profile, request.IsActive);
        var now = InstantPrecision.ForPersistence(clock.GetUtcNow());
        db.AuditEntries.Add(AuditSnapshots.Create(null, "User", user.Id, actor, now, before, AuditSnapshots.Capture(UserProjection.Snapshot(user)),
            request.Reason.Trim(), AuditOperation.Corrected));
        process.Step("Persisting", "Gravando alteração e histórico na mesma transação");
        // Renovar o security stamp revoga refresh tokens; o access token vigente expira em minutos e gravações conferem a situação.
        var saved = accessChanged ? await users.UpdateSecurityStampAsync(user) : await users.UpdateAsync(user);
        if (!saved.Succeeded)
            return Reject(Error.Conflict("record.version_conflict", string.Join(" ", saved.Errors.Select(x => x.Description).Distinct())));
        process.Event("UserUpdated", "Updated", "Conta {UserId} alterada; sessões revogadas: {SessionsRevoked}", user.Id, accessChanged);
        return process.Complete(Result<UserResponse>.Success(UserProjection.Map(user, now)));
    }
}
