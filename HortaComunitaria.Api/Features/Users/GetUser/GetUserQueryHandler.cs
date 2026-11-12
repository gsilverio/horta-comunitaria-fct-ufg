using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Features.Users.Shared;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Users.GetUser;

/// <summary>Consulta uma conta de acesso; o autor técnico do protótipo não é exposto.</summary>
internal sealed class GetUserQueryHandler(AppDbContext db, TimeProvider clock)
{
    public async Task<Result<UserResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsDemonstration, cancellationToken);
        return user is null
            ? Result<UserResponse>.Failure(new Error("resource.not_found", "Usuário não encontrado.", ErrorCategory.NotFound))
            : Result<UserResponse>.Success(UserProjection.Map(user, clock.GetUtcNow()));
    }
}
