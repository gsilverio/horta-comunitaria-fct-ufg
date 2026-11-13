using System.Threading.RateLimiting;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HortaComunitaria.Api.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity com bearer token nativo: perfis Adm/Operador, lockout, política de senha, chaves do Data
/// Protection persistidas, fallback policy que exige autenticação em toda rota e rate limit das rotas de autenticação.
/// </summary>
internal static class IdentityExtensions
{
    /// <summary>Nome da política de rate limit aplicada a login, renovação, logout e troca de senha.</summary>
    internal const string AuthRateLimitPolicy = "auth";

    internal static IServiceCollection AddAppIdentity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<AuthTokenOptions>().Bind(configuration.GetSection("Authentication"))
            .Validate(o => o.AccessTokenLifetime > TimeSpan.Zero && o.AccessTokenLifetime <= TimeSpan.FromHours(1),
                "Authentication:AccessTokenLifetime deve ficar entre 0 e 1 hora.")
            .Validate(o => o.RefreshTokenLifetime > o.AccessTokenLifetime && o.RefreshTokenLifetime <= TimeSpan.FromDays(30),
                "Authentication:RefreshTokenLifetime deve ser maior que o access token e no máximo 30 dias.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.KeysPath), "Configure Authentication:KeysPath.")
            .Validate(o => o.RateLimitPerMinute > 0, "Authentication:RateLimitPerMinute deve ser positivo.")
            .ValidateOnStart();
        services.AddOptions<BootstrapAdministratorOptions>().Bind(configuration.GetSection("Bootstrap"));

        // Lido da configuração durante o registro: o diretório é necessário para o Data Protection.
        var keysPath = configuration["Authentication:KeysPath"] is { Length: > 0 } configured ? configured : new AuthTokenOptions().KeysPath;
        services.AddDataProtection().SetApplicationName("HortaComunitaria")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(Path.Combine(environment.ContentRootPath, keysPath))));

        services.AddAuthentication(IdentityConstants.BearerScheme).AddBearerToken(IdentityConstants.BearerScheme);
        services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme)
            .Configure<IOptions<AuthTokenOptions>>((bearer, tokens) =>
            {
                bearer.BearerTokenExpiration = tokens.Value.AccessTokenLifetime;
                bearer.RefreshTokenExpiration = tokens.Value.RefreshTokenLifetime;
            });
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder(IdentityConstants.BearerScheme).RequireAuthenticatedUser().Build())
            .AddPolicy(AuthorizationPolicies.Administrator, policy => policy
                .AddAuthenticationSchemes(IdentityConstants.BearerScheme).RequireAuthenticatedUser()
                .RequireRole(nameof(UserProfile.Administrator)));

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = PasswordPolicy.MinimumLength;
                options.Password.RequiredUniqueChars = PasswordPolicy.RequiredUniqueChars;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddErrorDescriber<PtBrIdentityErrorDescriber>()
            .AddPasswordValidator<CommonPasswordValidator>();

        services.AddRateLimiter(_ => { });
        services.AddOptions<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>()
            .Configure<IOptions<AuthTokenOptions>>((limiter, tokens) =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.OnRejected = WriteRejectionAsync;
                limiter.AddPolicy(AuthRateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = tokens.Value.RateLimitPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            });

        services.AddHttpContextAccessor();
        services.AddScoped<AuthTokenService>();
        services.AddScoped<CurrentAuthor>();
        services.AddScoped<AdministratorBootstrap>();
        services.AddScoped<TestAdministratorsSeed>();
        return services;
    }

    /// <summary>Autenticação, autorização, rate limit e <c>no-store</c> nas respostas de credenciais.</summary>
    internal static WebApplication UseAppIdentity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(RefreshTokenCookie.Path) || context.Request.Path.StartsWithSegments("/api/v1/users"))
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.CacheControl = "no-store";
                    context.Response.Headers.Pragma = "no-cache";
                    return Task.CompletedTask;
                });
            return next(context);
        });
        return app;
    }

    private static async ValueTask WriteRejectionAsync(Microsoft.AspNetCore.RateLimiting.OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        var problem = new ProblemDetails { Status = 429, Title = "Muitas tentativas. Aguarde um minuto e tente novamente." };
        problem.Extensions["code"] = "auth.rate_limited";
        problem.Extensions["traceId"] = http.Items["TraceId"] ?? http.TraceIdentifier;
        await http.RequestServices.GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
    }
}
