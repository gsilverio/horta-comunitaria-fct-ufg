using System.Diagnostics;

namespace HortaComunitaria.Api.Infrastructure.Observability;

/// <summary>Identidade e progresso do caso de uso, preservados até o encerramento da requisição.</summary>
internal sealed class ProcessContext(string name)
{
    private const string ActivityKey = "HortaComunitaria.ProcessContext";
    private readonly long started = Stopwatch.GetTimestamp();
    private readonly Dictionary<string, object?> resources = new();

    internal string Name { get; } = name;
    internal Guid Id { get; } = Guid.CreateVersion7();
    internal string StepName { get; private set; } = "ValidatingRequest";
    internal int StepNumber { get; private set; }
    internal double ElapsedMs => Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    internal static ProcessContext? Current => Activity.Current?.GetCustomProperty(ActivityKey) as ProcessContext;

    internal static void SetCurrent(ProcessContext? context) => Activity.Current?.SetCustomProperty(ActivityKey, context);

    internal void AddResources(IDictionary<string, object?>? context)
    {
        if (context is null) return;
        foreach (var property in context) resources[property.Key] = property.Value;
    }

    internal void Advance(string stepName)
    {
        StepName = stepName;
        StepNumber++;
    }

    internal Dictionary<string, object?> Properties(string phase) => new(resources)
    {
        ["Module"] = Name.Split('.')[0],
        ["ProcessName"] = Name,
        ["ProcessId"] = Id,
        ["Phase"] = phase,
        ["StepName"] = StepName,
        ["StepNumber"] = StepNumber
    };
}
