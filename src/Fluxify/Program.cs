using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

const string json = """
                    {
                        "ServiceKey": "RootRouterStep",
                        "Children": [
                            {
                                "ServiceKey": "FallbackStep",
                                "RouteKey": "fallback"
                            },
                            {
                                "ServiceKey": "SupportStep",
                                "RouteKey": "support"
                            },        
                            {
                                "ServiceKey": "BusinessRouterStep",
                                "RouteKey": "business",
                                "Children": [
                                    {
                                        "ServiceKey": "InSeasonStep",
                                        "RouteKey": "in-season"
                                    },
                                    {
                                        "ServiceKey": "PreSeasonStep",
                                        "RouteKey": "pre-season"
                                    }
                                ]
                            }
                        ]
                    }
                    """;

var services = new ServiceCollection();
services.AddLogging(cfg => cfg.AddConsole().SetMinimumLevel(LogLevel.Debug));
services.AddSteps<Program>();

await using var serviceProvider = services.BuildServiceProvider();
LoggerBase.LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

var plan = JsonExecutionPlanLoader.Load(json, serviceProvider);

var input = Console.ReadLine();
if (string.IsNullOrWhiteSpace(input))
{
    return;
}

var context = new ExecutionPlanContext(input);

var runner = new ExecutionPlanRunner();
await runner.ExecuteAsync(context, plan);

Console.WriteLine(context.GetOutput<string>());

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
}

public class ExecutionPlanRunner : LoggerBase
{
    public async Task ExecuteAsync(ExecutionPlanContext context, ExecutionPlan plan)
    {
        var currentStep = plan.Root;

        while (currentStep is not null)
        {
            if (currentStep is RouterStepBase routerStep)
            {
                Logger.LogDebug("Executing router step {StepName}", currentStep.GetType().Name);

                await currentStep.ExecuteAsync(context);

                var routeKey = context.LastRouteKey!;

                Logger.LogDebug("Router step {StepName} determined route key {RouteKey}", currentStep.GetType().Name,
                    routeKey);

                if (!plan.Children.TryGetValue(routerStep, out var childSteps))
                {
                    throw new InvalidOperationException(
                        $"Router {routerStep.GetType().Name} has no registered children.");
                }

                if (!childSteps.TryGetValue(routeKey, out var routedStep))
                {
                    throw new InvalidOperationException(
                        $"Route key '{routeKey}' not found for router '{routerStep.GetType().Name}'.");
                }

                currentStep = routedStep;
            }
            else
            {
                Logger.LogDebug("Executing action step {StepName}", currentStep.GetType().Name);

                await currentStep.ExecuteAsync(context);

                currentStep = null;
            }
        }
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSteps<T>(this IServiceCollection services)
    {
        var types = typeof(T).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IStep).IsAssignableFrom(t));

        foreach (var type in types)
        {
            services.AddKeyedTransient(typeof(IStep), type.Name, type);
        }

        return services;
    }
}

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

public class StepDefinition
{
    public required string ServiceKey { get; init; }
    public string? RouteKey { get; init; }
    public IEnumerable<StepDefinition>? Children { get; init; }
}

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

public class ExecutionPlanContext
{
    /// <summary>
    /// Original input to the plan.
    /// </summary>
    public string Input { get; }

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

    private readonly IList<ExecutionRecord> _executionRecords = [];

    public ExecutionPlanContext(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentNullException(nameof(input));
        }
        Input = input;
        StartedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<ExecutionRecord> ExecutionRecords => _executionRecords.AsReadOnly();

    public void AddExecutionRecord(string stepName, DateTime startedAt, DateTime finishedAt, object? output = null, string? routeKey = null)
    {
        _executionRecords.Add(new ExecutionRecord(stepName, startedAt, finishedAt, Input, output, routeKey));
    }

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

public abstract class LoggerBase
{
    private ILogger? _logger;

    public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());
}

public interface IStep
{
    Task ExecuteAsync(ExecutionPlanContext context);
}

public interface IRouterStep : IStep;

public abstract class RouterStepBase : IRouterStep
{
    public async Task ExecuteAsync(ExecutionPlanContext context)
    {
        var startedAt = DateTime.UtcNow;

        context.LastRouteKey = await GetRouteKeyAsync(context.Input, context) ??
                           throw new InvalidOperationException(
                               $"{GetType().Name} could not determine a valid route key.");

        context.AddExecutionRecord(GetType().Name, startedAt, DateTime.UtcNow, routeKey: context.LastRouteKey);
    }

    protected abstract Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context);
}

public interface IActionStep : IStep;

public abstract class ActionStepBase<TOutput> : IActionStep
{
    public async Task ExecuteAsync(ExecutionPlanContext context)
    {
        var startedAt = DateTime.UtcNow;

        context.Output = await ExecuteCoreAsync(context.Input, context);

        context.AddExecutionRecord(GetType().Name, startedAt, DateTime.UtcNow, output: context.Output);
    }

    protected abstract Task<TOutput?> ExecuteCoreAsync(string input, ExecutionPlanContext context);
}

public class RootRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context)
    {
        string? routeKey = null;

        if (input.Contains("hi", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "fallback";
        }
        else if (input.Contains("support", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "support";
        }
        else if (input.Contains("in-season", StringComparison.OrdinalIgnoreCase) ||
                 input.Contains("pre-season", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "business";
        }

        return Task.FromResult(routeKey);
    }
}

public class FallbackStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("How are you?");
    }
}

public class SupportRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("level-1");
    }
}

public class SupportStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with support?");
    }
}

public class FirstLevelSupportStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with level-1 support?");
    }
}

public class BusinessRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context)
    {
        string? routeKey = null;

        if (input.Contains("in-season", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "in-season";
        }
        else if (input.Contains("pre-season", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "pre-season";
        }

        return Task.FromResult(routeKey);
    }
}

public class InSeasonStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with in-season?");
    }
}

public class PreSeasonStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with pre-season?");
    }
}

public interface IFluxify;