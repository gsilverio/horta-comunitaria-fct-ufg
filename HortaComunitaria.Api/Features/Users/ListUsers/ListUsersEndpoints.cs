using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Endpoints;
using HortaComunitaria.Api.Infrastructure.Identity;

namespace HortaComunitaria.Api.Features.Users.ListUsers;

internal static class ListUsersEndpoints
{
    internal static RouteGroupBuilder MapListUsersEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/users", async (ListUsersQueryHandler handler, HttpContext context, CancellationToken cancellationToken,
                int page = 1, int pageSize = PaginationRequest.DefaultPageSize, bool? isActive = null) =>
                (await handler.HandleAsync(new PaginationRequest(page, pageSize), isActive, cancellationToken)).ToHttpResult(context))
            .RequireAdministrator()
            .WithName("Users.ListUsers").WithTags("Usuários")
            .WithSummary("Listar usuários")
            .WithDescription("Contas de acesso ordenadas por nome, com perfil e situação. Exclusivo do perfil Adm.")
            .Produces<Page<UserResponse>>().ProducesValidationProblem().ProducesProblem(401).ProducesProblem(403);
        return group;
    }
}
