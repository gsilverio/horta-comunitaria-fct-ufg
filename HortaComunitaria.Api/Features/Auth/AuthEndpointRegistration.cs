using HortaComunitaria.Api.Features.Auth.ChangePassword;
using HortaComunitaria.Api.Features.Auth.GetCurrentUser;
using HortaComunitaria.Api.Features.Auth.Login;
using HortaComunitaria.Api.Features.Auth.Logout;
using HortaComunitaria.Api.Features.Auth.RefreshSession;

namespace HortaComunitaria.Api.Features.Auth;

internal static class AuthEndpointRegistration
{
    internal static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group) =>
        group.MapLoginEndpoints().MapRefreshSessionEndpoints().MapLogoutEndpoints().MapGetCurrentUserEndpoints().MapChangePasswordEndpoints();
}
