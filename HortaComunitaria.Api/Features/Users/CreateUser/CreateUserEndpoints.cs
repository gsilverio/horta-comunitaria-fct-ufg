using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Users.CreateUser;

internal static class CreateUserEndpoints
{
    internal static RouteGroupBuilder MapCreateUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/users", async (CreateUserRequest request, CreateUserCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(request, cancellationToken)).ToHttpResult(context, value => Results.Created($"/api/v1/users/{value.Id}", value)))
            .RequireAdministrator()
            .WithName("Users.CreateUser").WithTags("Usuários")
            .WithSummary("Criar usuário")
            .WithDescription("Cria conta ativa com senha inicial e perfil; a pessoa troca a senha no primeiro acesso. Exclusivo do perfil Adm.")
            .Produces<UserResponse>(201).ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(409);
        return group;
    }
}
