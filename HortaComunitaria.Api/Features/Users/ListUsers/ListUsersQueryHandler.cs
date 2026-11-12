using HortaComunitaria.Api.Common.Pagination;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Users.ListUsers;

/// <summary>Lista as contas de acesso por nome; o autor técnico do protótipo não aparece.</summary>
internal sealed class ListUsersQueryHandler(AppDbContext db, TimeProvider clock, ILogger<ListUsersQueryHandler> logger)
{
    public async Task<Result<Page<UserResponse>>> HandleAsync(PaginationRequest pagination, bool? isActive, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Users.ListUsers");
        var valid = pagination.Validate();
        if (!valid.IsSuccess) return process.Complete(Result<Page<UserResponse>>.Failure(valid.Error));
        var query = db.Users.AsNoTracking().Where(x => !x.IsDemonstration);
        if (isActive is { } active) query = query.Where(x => x.IsActive == active);
        process.Step("Querying", "Consultando usuários; página {Page}", pagination.Page);
        var count = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id).Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var items = rows.Select(x => UserProjection.Map(x, now)).ToArray();
        return process.Complete(Result<Page<UserResponse>>.Success(new(items, pagination.Page, pagination.PageSize, count)), items.Length);
    }
}
