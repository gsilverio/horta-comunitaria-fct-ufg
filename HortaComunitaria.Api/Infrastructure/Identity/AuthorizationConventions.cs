using Microsoft.AspNetCore.Mvc;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>Policies nomeadas de autorização.</summary>
public static class AuthorizationPolicies
{
    /// <summary>Exige o perfil <see cref="UserProfile.Administrator"/>.</summary>
    public const string Administrator = nameof(Administrator);
}

/// <summary>Marca rotas permitidas enquanto a troca da senha inicial está pendente.</summary>
public sealed class AllowPendingPasswordChangeAttribute : Attribute;

/// <summary>Atalhos de metadados de autenticação/autorização para os endpoints.</summary>
public static class AuthEndpointConventions
{
    /// <summary>Restringe a ação ao perfil Adm (estrutura, catálogos e usuários).</summary>
    public static TBuilder RequireAdministrator<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(AuthorizationPolicies.Administrator);

    /// <summary>Permite a rota enquanto a troca de senha está pendente.</summary>
    public static TBuilder AllowPendingPasswordChange<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AllowPendingPasswordChangeAttribute());
}

/// <summary>
/// Bloqueia no servidor (403) toda rota não marcada com <see cref="AllowPendingPasswordChangeAttribute"/>
/// para quem ainda usa a senha inicial.
/// </summary>
internal sealed class PasswordChangeRequiredFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (http.User.HasClaim(AppClaimTypes.MustChangePassword, "true") &&
            http.GetEndpoint()?.Metadata.GetMetadata<AllowPendingPasswordChangeAttribute>() is null)
        {
            var problem = new ProblemDetails { Status = 403, Title = "Defina uma nova senha antes de continuar." };
            problem.Extensions["code"] = "auth.password_change_required";
            problem.Extensions["traceId"] = http.Items["TraceId"] ?? http.TraceIdentifier;
            return Results.Problem(problem);
        }
        return await next(context);
    }
}
