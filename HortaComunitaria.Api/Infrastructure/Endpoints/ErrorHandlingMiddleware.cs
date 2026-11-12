using Microsoft.AspNetCore.Mvc;
using HortaComunitaria.Api.Infrastructure.Observability;

namespace HortaComunitaria.Api.Infrastructure.Endpoints;

internal sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problems)
    {
        try
        {
            await next(context);
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted &&
                context.Response.ContentLength is null)
            {
                var (code, title) = context.Response.StatusCode switch
                {
                    401 => ("auth.unauthenticated", "Entre para continuar."),
                    403 => ("auth.forbidden", "Seu perfil não permite esta ação."),
                    404 => ("resource.not_found", "Recurso não encontrado."),
                    _ => ("validation.invalid_request", "Entrada incompatível com o contrato.")
                };
                await WriteProblemAsync(context, problems, context.Response.StatusCode, code, title);
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            using var scope = logger.BeginScope(FailureProperties("Cancelled"));
            logger.LogDebug("Requisição cancelada pelo cliente; evento {EventName}, resultado {Outcome}", "HttpRequestCancelled", "Cancelled");
            if (!context.Response.HasStarted) context.Response.StatusCode = 499;
        }
        catch (HortaComunitaria.Api.Infrastructure.Identity.InactiveSessionException) when (!context.Response.HasStarted)
        {
            using var scope = logger.BeginScope(FailureProperties("Rejected"));
            logger.LogWarning("Gravação recusada: sessão sem usuário ativo; evento {EventName}, resultado {Outcome}, código {ErrorCode}",
                "SessionRejected", "Rejected", "auth.session_expired");
            context.Response.Clear();
            await WriteProblemAsync(context, problems, 401, "auth.session_expired", "Sessão expirada. Entre novamente.");
        }
        catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
        {
            using var scope = logger.BeginScope(FailureProperties("Rejected"));
            logger.LogWarning("Entrada rejeitada durante o binding HTTP; evento {EventName}, resultado {Outcome}, código {ErrorCode}",
                "RequestBindingRejected", "Rejected", "validation.invalid_request");
            await WriteProblemAsync(context, problems, exception.StatusCode, "validation.invalid_request",
                "Entrada incompatível com o contrato.");
        }
        catch (Exception exception)
        {
            // Com headers enviados, o middleware HTTP registra a exceção propagada; evita duplicar o stack trace.
            if (context.Response.HasStarted) throw;
            using var scope = logger.BeginScope(FailureProperties("Failed"));
            logger.LogError(exception, "Falha inesperada ao executar o processo; evento {EventName}, resultado {Outcome}, código {ErrorCode}",
                "UnhandledException", "Failed", "server.unexpected_error");
            context.Response.Clear();
            await WriteProblemAsync(context, problems, 500, "server.unexpected_error",
                "Não foi possível concluir a operação.");
        }
    }

    private static Dictionary<string, object?> FailureProperties(string phase)
    {
        var process = ProcessContext.Current;
        var properties = process?.Properties(phase) ?? new Dictionary<string, object?> { ["Phase"] = phase };
        properties["ElapsedMs"] = process?.ElapsedMs;
        return properties;
    }

    private static async Task WriteProblemAsync(HttpContext context, IProblemDetailsService problems,
        int status, string code, string title)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.Items["TraceId"] ?? context.TraceIdentifier;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
