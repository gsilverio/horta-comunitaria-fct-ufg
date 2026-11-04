using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Infrastructure.Identity;

internal sealed class DemoAuthorContext(AppDbContext db)
{
    internal async Task<Guid> ResolveAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(x => x.AuthenticationIdentifier == "demo:operator" && x.IsActive)
            .Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken) ??
        throw new InvalidOperationException("Autor demonstrativo ausente; prepare o ambiente conforme o guia Get Started.");
}
