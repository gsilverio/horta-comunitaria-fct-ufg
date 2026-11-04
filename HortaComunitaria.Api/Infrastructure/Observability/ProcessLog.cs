using HortaComunitaria.Api.Common.Results;

namespace HortaComunitaria.Api.Infrastructure.Observability;

/// <summary>Escopo de um processo, sem dependência de observabilidade no domínio.</summary>
internal sealed class ProcessLog : IDisposable
{
    private readonly ILogger logger;
    private readonly IDisposable? scope;
    private readonly ProcessContext context;
    private readonly ProcessContext? previous;

    public ProcessLog(ILogger logger, string processName, IDictionary<string, object?>? context = null)
    {
        this.logger = logger;
        previous = ProcessContext.Current;
        this.context = previous?.Name == processName ? previous : new ProcessContext(processName);
        this.context.AddResources(context);
        ProcessContext.SetCurrent(this.context);
        scope = logger.BeginScope(this.context.Properties("Started"));
        using var eventScope = logger.BeginScope(new Dictionary<string, object?> { ["EventName"] = "ProcessStarted" });
        logger.LogInformation("Iniciando processo {ProcessName}", processName);
    }

    /// <summary>Registra uma etapa significativa sem copiar o formulário ou detalhes de SQL.</summary>
    internal void Step(string stepName, string messageTemplate, params object?[] values)
    {
        context.Advance(stepName);
        using var stepScope = logger.BeginScope(context.Properties("Executing"));
        using var eventScope = logger.BeginScope(new Dictionary<string, object?> { ["EventName"] = "ProcessStep" });
        logger.LogInformation(messageTemplate, values);
    }

    /// <summary>Registra o resultado específico do caso de uso após sua confirmação.</summary>
    internal void Event(string eventName, string outcome, string messageTemplate, params object?[] values)
    {
        var properties = context.Properties("Executing");
        properties["EventName"] = eventName;
        properties["Outcome"] = outcome;
        using var eventScope = logger.BeginScope(properties);
        logger.LogInformation(messageTemplate, values);
    }

    public Result<T> Complete<T>(Result<T> result, int? resultCount = null)
    {
        var properties = context.Properties(result.IsSuccess ? "Completed" : "Rejected");
        properties["EventName"] = result.IsSuccess ? "ProcessCompleted" : "ProcessRejected";
        properties["Outcome"] = result.IsSuccess ? "Succeeded" : "Rejected";
        properties["ElapsedMs"] = context.ElapsedMs;
        properties["ResultCount"] = resultCount;
        properties["ErrorCode"] = result.IsSuccess ? null : result.Error.Code;
        using var eventScope = logger.BeginScope(properties);
        if (result.IsSuccess)
            logger.LogInformation("Processo {ProcessName} concluído em {ElapsedMs:0.000} ms", context.Name, context.ElapsedMs);
        else
            logger.LogWarning("Processo {ProcessName} rejeitado na etapa {StepName}: {ErrorCode}",
                context.Name, context.StepName, result.Error.Code);
        return result;
    }

    public void Dispose()
    {
        scope?.Dispose();
        ProcessContext.SetCurrent(previous);
    }
}
