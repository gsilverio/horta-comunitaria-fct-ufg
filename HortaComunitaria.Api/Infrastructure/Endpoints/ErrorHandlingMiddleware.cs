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
            var (field, reason) = BindingFailure(exception);
            var properties = FailureProperties("Rejected");
            properties["ErrorFields"] = field is null ? null : new[] { field };
            using var scope = logger.BeginScope(properties);
            // Registra o campo e o tipo de falha do binding; o valor recebido nunca entra no log.
            logger.LogWarning("Entrada rejeitada durante o binding HTTP; evento {EventName}, resultado {Outcome}, código {ErrorCode}, campo {ErrorField}, motivo {BindingReason}",
                "RequestBindingRejected", "Rejected", "validation.invalid_request", field ?? "-", reason);
            await WriteProblemAsync(context, problems, exception.StatusCode, "validation.invalid_request",
                "Entrada incompatível com o contrato.", field is null ? null : new Dictionary<string, string[]> { [field] = ["Valor em formato incompatível com o campo."] });
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

    /// <summary>Campo (caminho JSON em camelCase) e motivo resumido da falha de binding, sem o valor enviado.</summary>
    private static (string? Field, string Reason) BindingFailure(BadHttpRequestException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.Text.Json.JsonException json)
            {
                var path = json.Path is { Length: > 0 } p ? p.TrimStart('$').TrimStart('.') : null;
                var reason = json.Message.Contains("missing required properties", StringComparison.OrdinalIgnoreCase) ? MissingRequired(json.Message)
                    : json.Message.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ? "tipo ou formato incompatível"
                    : "JSON inválido";
                return (string.IsNullOrEmpty(path) ? null : path, reason);
            }
        }
        return (null, exception.Message.Contains("body", StringComparison.OrdinalIgnoreCase) ? "corpo ausente ou ilegível" : "parâmetro de rota ou consulta inválido");
    }

    private static string MissingRequired(string message)
    {
        var start = message.IndexOf("including:", StringComparison.OrdinalIgnoreCase);
        return start < 0 ? "propriedade obrigatória ausente" : "propriedade obrigatória ausente: " + message[(start + 10)..].Trim().TrimEnd('.');
    }

    private static async Task WriteProblemAsync(HttpContext context, IProblemDetailsService problems,
        int status, string code, string title, IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = title };
        if (errors is not null) problem.Extensions["errors"] = errors;
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.Items["TraceId"] ?? context.TraceIdentifier;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
