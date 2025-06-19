namespace Fluxify;

public class ExecutionRecord
{
    public string StepName { get; }
    public DateTime StartedAt { get; }
    public DateTime FinishedAt { get; }
    public string Input { get; }
    public object? Output { get; }
    public string? RouteKey { get; }

    public ExecutionRecord(string stepName, DateTime startedAt, DateTime finishedAt, string input, object? output,
        string? routeKey)
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentNullException(nameof(stepName));
        }
        StepName = stepName;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        Input = input;
        Output = output;
        RouteKey = routeKey;
    }
}