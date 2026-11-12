using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Users.UpdateUser;

internal static class UpdateUserEndpoints
{
    internal static RouteGroupBuilder MapUpdateUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/users/{id:guid}", async (Guid id, UpdateUserRequest request, UpdateUserCommandHandler handler, HttpContext context, CancellationToken cancellationToken) =>
                (await handler.HandleAsync(id, request, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator()
            .WithName("Users.UpdateUser").WithTags("Usuários")
            .WithSummary("Alterar usuário")
            .WithDescription("Altera nome, perfil e situação com versão esperada e motivo. Mudança de perfil ou situação encerra as sessões da conta. " +
                             "O Adm não pode desativar nem rebaixar a própria conta. Não redefine senha. Exclusivo do perfil Adm.")
            .Produces<UserResponse>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
        return group;
    }
}
