using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Claims próprias da aplicação gravadas nos tokens.</summary>
public static class AppClaimTypes
{
    /// <summary>Presente (valor <c>true</c>) enquanto o usuário precisa trocar a senha inicial.</summary>
    public const string MustChangePassword = "horta:must_change_password";
}

/// <summary>Acrescenta ao principal do Identity o perfil (claim de papel) e a troca de senha pendente.</summary>
internal sealed class AppUserClaimsPrincipalFactory(UserManager<AppUser> userManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Profile.ToString()));
        if (user.MustChangePassword) identity.AddClaim(new Claim(AppClaimTypes.MustChangePassword, "true"));
        return identity;
    }
}
