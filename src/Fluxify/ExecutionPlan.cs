using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluxify;

public class ExecutionPlan
{
    public IStep Root { get; set; } = null!;
    public Dictionary<RouterStepBase, Dictionary<string, IStep>> Children { get; } = [];

    public string AsJson()
    {
        var definition = BuildStepDefinition(Root, null);
        return JsonSerializer.Serialize(definition, new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        });
    }

    private StepDefinition BuildStepDefinition(IStep step, string? routeKey)
    {
        if (step is RouterStepBase router && Children.TryGetValue(router, out var children))
        {
            return new StepDefinition
            {
                ServiceKey = step.GetType().Name,
                RouteKey = routeKey,
                Children = [.. children.Select(kvp => BuildStepDefinition(kvp.Value, kvp.Key))]
            };
        }

        return new StepDefinition
        {
            ServiceKey = step.GetType().Name,
            RouteKey = routeKey
        };
    }

    public IStep GetStep(Type type)
    {
        if (Root.GetType() == type)
        {
            return Root;
        }

        foreach (var child in Children.Values.SelectMany(c => c.Values))
        {
            if (child.GetType() == type)
            {
                return child;
            }
        }

        throw new InvalidOperationException($"Step of type {type.Name} not found in the execution plan.");
    }

    public T GetStep<T>() where T : IStep
    {
        return (T)GetStep(typeof(T));
    }
}