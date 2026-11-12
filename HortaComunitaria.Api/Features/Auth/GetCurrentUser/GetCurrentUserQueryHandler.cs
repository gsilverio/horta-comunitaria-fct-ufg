using System.Security.Claims;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Auth.Shared;
using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HortaComunitaria.Api.Features.Auth.GetCurrentUser;

/// <summary>Usuário da sessão atual.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Email">E-mail de acesso.</param>
/// <param name="DisplayName">Nome de exibição.</param>
/// <param name="Profile">Perfil de acesso: <c>Administrator</c> ou <c>Operator</c>.</param>
/// <param name="MustChangePassword">Quando verdadeiro, o cliente leva à troca de senha; o servidor bloqueia as demais rotas.</param>
internal sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, UserProfile Profile, bool MustChangePassword);

/// <summary>Consulta os dados atuais do usuário autenticado no banco, não apenas as claims do token.</summary>
internal sealed class GetCurrentUserQueryHandler(UserManager<AppUser> users)
{
    public async Task<Result<CurrentUserResponse>> HandleAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await users.GetUserAsync(principal);
        return user is { IsActive: true }
            ? Result<CurrentUserResponse>.Success(new(user.Id, user.Email ?? "", user.DisplayName, user.Profile, user.MustChangePassword))
            : Result<CurrentUserResponse>.Failure(AuthErrors.SessionExpired());
    }
}
