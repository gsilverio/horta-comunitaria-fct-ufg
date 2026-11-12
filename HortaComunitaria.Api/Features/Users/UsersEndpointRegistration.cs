using HortaComunitaria.Api.Features.Users.CreateUser;
using HortaComunitaria.Api.Features.Users.GetUser;
using HortaComunitaria.Api.Features.Users.ListUsers;
using HortaComunitaria.Api.Features.Users.UpdateUser;

namespace HortaComunitaria.Api.Features.Users;

internal static class UsersEndpointRegistration
{
    internal static RouteGroupBuilder MapUsersEndpoints(this RouteGroupBuilder group) =>
        group.MapListUsersEndpoints().MapGetUserEndpoints().MapCreateUserEndpoints().MapUpdateUserEndpoints();
}
