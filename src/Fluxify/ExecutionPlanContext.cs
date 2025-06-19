using Microsoft.SemanticKernel.ChatCompletion;

namespace Fluxify;

public class ExecutionPlanContext
{
    private string _input = null!;

    /// <summary>
    /// Original input to the plan.
    /// </summary>
    public string Input
    {
        get => _input;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Input));
            _input = value;
        }
    }

    /// <summary>
    /// Output set by the executed action step of type <see cref="ActionStepBase{R}"/>.
    /// </summary>
    public object? Output { get; set; }

    /// <summary>
    /// RouteKey set by the last executed step of type <see cref="RouterStepBase"/>.
    /// </summary>
    public string? LastRouteKey { get; set; }

    public DateTime StartedAt { get; }

    public DateTime? FinishedAt => _executionRecords.LastOrDefault()?.FinishedAt;

    public ChatHistory History { get; set; }

    private readonly IList<ExecutionRecord> _executionRecords = [];

    public ExecutionPlanContext(string input, ChatHistory? history = null)
    {
        Input = input;
        StartedAt = DateTime.UtcNow;
        History = history ?? [];
    }

    public IReadOnlyList<ExecutionRecord> ExecutionRecords => _executionRecords.AsReadOnly();

    public void AddExecutionRecord(string stepName, DateTime startedAt, DateTime finishedAt, object? output = null, string? routeKey = null)
    {
        _executionRecords.Add(new ExecutionRecord(stepName, startedAt, finishedAt, Input, output, routeKey));
    }


    public string? GetPreviousRouting(string stepName) => _executionRecords
            .Where(r => r.StepName == stepName)
            .OrderByDescending(r => r.FinishedAt)
            .FirstOrDefault()?.RouteKey;

    public IEnumerable<object> GetHistoryForPromptTemplate() => History.Select(h => new { h.Role, h.Content }).ToList();

    public T? GetOutput<T>()
    {
        return To<T>(Output);
    }

    private static T? To<T>(object? obj) =>
        obj switch
        {
            null => default,
            T value => value,
            _ => throw new InvalidCastException(
                $"Expected value of type '{typeof(T).Name}', but was '{obj.GetType().Name}'.")
        };
}