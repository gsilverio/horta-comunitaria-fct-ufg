using System.Security.Claims;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Sessão autenticada sem usuário ativo correspondente; traduzida para 401.</summary>
internal sealed class InactiveSessionException() : Exception("Sessão sem usuário ativo.");

/// <summary>
/// Resolve o autor das gravações pela identidade autenticada, nunca por dado enviado pelo cliente. Confere no banco
/// que o usuário continua ativo, pois o access token vigente não é revogado antes de expirar.
/// </summary>
internal sealed class CurrentAuthor(IHttpContextAccessor accessor, AppDbContext db)
{
    private Guid? resolved;

    /// <summary>Identificador do usuário autenticado e ativo.</summary>
    internal async Task<Guid> ResolveAsync(CancellationToken cancellationToken)
    {
        if (resolved is { } cached) return cached;
        var claim = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var id)) throw new InactiveSessionException();
        var active = await db.Users.AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive && !x.IsDemonstration, cancellationToken);
        if (!active) throw new InactiveSessionException();
        resolved = id;
        return id;
    }
}
