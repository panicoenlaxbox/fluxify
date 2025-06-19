using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxify;

public static class JsonExecutionPlanLoader
{
    public static ExecutionPlan Load(string json, IServiceProvider services)
    {
        var stepDefinition = JsonSerializer.Deserialize<StepDefinition>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        })!;

        var plan = new ExecutionPlan();
        plan.Root = Build(stepDefinition, plan, services);
        return plan;
    }

    private static IStep Build(StepDefinition definition, ExecutionPlan plan, IServiceProvider serviceProvider)
    {
        var step = serviceProvider.GetRequiredKeyedService<IStep>(definition.ServiceKey);

        if (step is RouterStepBase routerStep)
        {
            if (definition.Children is null)
            {
                throw new InvalidOperationException($"Router step {definition.ServiceKey} is missing children");
            }

            var children = new Dictionary<string, IStep>();
            foreach (var child in definition.Children)
            {
                if (string.IsNullOrWhiteSpace(child.RouteKey))
                {
                    throw new InvalidOperationException(
                        $"Child {child.ServiceKey} missing RouteKey for parent {definition.ServiceKey}");
                }

                children[child.RouteKey] = Build(child, plan, serviceProvider);
            }

            plan.Children[routerStep] = children;
        }
        else if (definition.Children?.Any() == true)
        {
            throw new InvalidOperationException($"Action step {definition.ServiceKey} cannot have children");
        }

        return step;
    }
}