using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Users.GetUser;

internal static class GetUserEndpoints
{
    internal static RouteGroupBuilder MapGetUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/users/{id:guid}", async (Guid id, GetUserQueryHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator()
            .WithName("Users.GetUser").WithTags("Usuários")
            .WithSummary("Consultar usuário")
            .WithDescription("Conta de acesso com perfil, situação e versão para alteração. Exclusivo do perfil Adm.")
            .Produces<UserResponse>().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404);
        return group;
    }
}
