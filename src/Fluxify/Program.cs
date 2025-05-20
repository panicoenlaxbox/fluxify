using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

var json = """
           {
               "ServiceKey": "RootRouterStep",
               "Children": [
                   {
                       "ServiceKey": "FallbackStep",
                       "RouteKey": "fallback"
                   },
                   {
                       "ServiceKey": "SupportStep",
                       "RouteKey": "support",
                       "Children": [
                           {
                               "ServiceKey": "FirstLevelSupportStep"
                           }
                       ]
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

//var json = """
//    {
//      "ServiceKey": "SupportStep",
//      "Children": [
//        { "ServiceKey": "FirstLevelSupportStep" }
//      ]
//    }
//    """;

var services = new ServiceCollection();
services.AddLogging(cfg => cfg.AddConsole().SetMinimumLevel(LogLevel.Debug));
services.AddSteps<Program>();

await using var serviceProvider = services.BuildServiceProvider();
LoggerBase.SetLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>());

var plan = JsonStepExecutionPlanLoader.Load(json, serviceProvider);
Console.WriteLine(plan.AsJson());

var input = Console.ReadLine();
if (string.IsNullOrWhiteSpace(input))
{
    return;
}

var context = new StepExecutionPlanContext(input);

var runner = new StepExecutionPlanRunner();
await runner.ExecuteAsync(context, plan);

Console.WriteLine(context.Output);

public class StepExecutionPlan
{
    public IStep Root { get; set; } = null!;
    public Dictionary<RouterStepBase, Dictionary<string, IStep>> ChildrenMap { get; } = [];
    public Dictionary<IStep, IStep> NextMap { get; } = [];

    public string AsJson()
    {
        var definition = BuildDefinition(Root, null);
        return JsonSerializer.Serialize(definition, new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        });
    }

    private StepDefinition BuildDefinition(IStep step, string? routeKey)
    {
        if (step is RouterStepBase router && ChildrenMap.TryGetValue(router, out var children))
        {
            return new StepDefinition
            {
                ServiceKey = step.GetType().Name,
                RouteKey = routeKey,
                Children = [.. children.Select(kvp => BuildDefinition(kvp.Value, kvp.Key))]
            };
        }
        else if (NextMap.TryGetValue(step, out var nextStep))
        {
            return new StepDefinition
            {
                ServiceKey = step.GetType().Name,
                RouteKey = routeKey,
                Children = [BuildDefinition(nextStep, null)]
            };
        }

        return new StepDefinition
        {
            ServiceKey = step.GetType().Name,
            RouteKey = routeKey
        };
    }
}

public class StepExecutionPlanRunner : LoggerBase
{
    public async Task ExecuteAsync(StepExecutionPlanContext context, StepExecutionPlan plan)
    {
        var currentStep = plan.Root;

        while (currentStep is not null)
        {
            if (currentStep is RouterStepBase routerStep)
            {
                Logger.LogDebug("Executing router step {StepName}", currentStep.GetType().Name);

                await currentStep.ExecuteAsync(context);

                var routeKey = context.RouteKey ?? throw new InvalidOperationException("Route key was not determined.");

                Logger.LogDebug("Router step {StepName} determined route key {RouteKey}", currentStep.GetType().Name,
                    routeKey);

                if (!plan.ChildrenMap.TryGetValue(routerStep, out var children))
                {
                    throw new InvalidOperationException(
                        $"Router {routerStep.GetType().Name} has no registered children.");
                }

                if (!children.TryGetValue(routeKey, out var routedStep))
                {
                    throw new InvalidOperationException(
                        $"Route key '{routeKey}' not found for router '{routerStep.GetType().Name}'.");
                }

                currentStep = routedStep;
            }
            else
            {
                plan.NextMap.TryGetValue(currentStep, out var nextStep);

                Logger.LogDebug("Executing action step {StepName}", currentStep.GetType().Name);

                await currentStep.ExecuteAsync(context);
                currentStep = nextStep;
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

public static class JsonStepExecutionPlanLoader
{
    public static StepExecutionPlan Load(string json, IServiceProvider services)
    {
        var definition = JsonSerializer.Deserialize<StepDefinition>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        })!;

        var plan = new StepExecutionPlan();
        plan.Root = Build(definition, plan, services);
        return plan;
    }

    private static IStep Build(StepDefinition definition, StepExecutionPlan plan, IServiceProvider serviceProvider)
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

                var builtChild = Build(child, plan, serviceProvider);
                children[child.RouteKey] = builtChild;
            }

            plan.ChildrenMap[routerStep] = children;
        }
        else if (step is IActionStep && definition.Children?.Any() == true)
        {
            if (definition.Children.Count() != 1)
            {
                throw new InvalidOperationException($"Step {definition.ServiceKey} must have exactly one child.");
            }

            var nextStep = Build(definition.Children.First(), plan, serviceProvider);
            plan.NextMap[step] = nextStep;
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

public class StepExecutionRecord
{
    public string StepName { get; }
    public DateTime StartedAt { get; }
    public DateTime FinishedAt { get; }
    public object? Input { get; }
    public object? Output { get; }
    public string? RouteKey { get; }
    public Dictionary<string, object>? Metadata { get; }

    public StepExecutionRecord(string stepName, DateTime startedAt, DateTime finishedAt, object? input, object? output,
        string? routeKey, Dictionary<string, object>? metadata)
    {
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Output = output;
        RouteKey = routeKey;
        Metadata = metadata;
    }
}

public class StepExecutionPlanContext
{
    /// <summary>
    /// Original input to the plan.
    /// </summary>
    public object? Input { get; }

    /// <summary>
    /// Output set by the last executed step of type <see cref="ActionStepBase{T}"/>.
    /// </summary>
    public object? Output { get; set; }

    /// <summary>
    /// RouteKey set by the last executed step of type <see cref="RouterStepBase"/>.
    /// </summary>
    public string? RouteKey { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }

    private readonly IList<StepExecutionRecord> _executionRecords = [];

    public IReadOnlyList<StepExecutionRecord> ExecutionRecords => _executionRecords.AsReadOnly();

    public StepExecutionPlanContext(object? input)
    {
        Input = input;
    }

    public void AddExecutionRecord(string stepName, DateTime startedAt, DateTime finishedAt, object? input,
        object? output = null, string? routeKey = null, Dictionary<string, object>? metadata = null)
    {
        _executionRecords.Add(new StepExecutionRecord(stepName, startedAt, finishedAt, input, output, routeKey,
            metadata));
    }

    public T? GetInput<T>()
    {
        return To<T>(Input);
    }

    public T? GetOutput<T>()
    {
        return To<T>(Output);
    }

    private static T? To<T>(object? obj)
    {
        if (obj is null)
        {
            return default;
        }

        if (obj is T value)
        {
            return value;
        }

        throw new InvalidCastException($"Expected value of type '{typeof(T).Name}', but was '{obj.GetType().Name}'.");
    }
}

public abstract class LoggerBase
{
    private ILogger? _logger;

    private static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    public static void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        LoggerFactory = loggerFactory;
    }

    protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());
}

public interface IStep
{
    Task ExecuteAsync(StepExecutionPlanContext context);
}

public interface IRouterStep : IStep;

public abstract class RouterStepBase : IRouterStep
{
    public async Task ExecuteAsync(StepExecutionPlanContext context)
    {
        var startedAt = DateTime.UtcNow;

        string input = (string)(context.ExecutionRecords.LastOrDefault()?.Output ?? context.GetInput<string>()!);

        var routeKey = await GetRouteKeyAsync(input, context);

        if (routeKey is null)
        {
            throw new InvalidOperationException($"{GetType().Name} could not determine a valid route key.");
        }

        context.RouteKey = routeKey;

        context.AddExecutionRecord(GetType().Name, startedAt, DateTime.UtcNow, input, routeKey: context.RouteKey);
    }

    protected abstract Task<string?> GetRouteKeyAsync(string input, StepExecutionPlanContext context);
}

public interface IActionStep : IStep;

public abstract class ActionStepBase<T> : IActionStep
{
    public async Task ExecuteAsync(StepExecutionPlanContext context)
    {
        var startedAt = DateTime.UtcNow;

        object? input = context.Input;
        foreach (var record in context.ExecutionRecords.Reverse())
        {
            if (record.Output is not null)
            {
                input = record.Output;
                break;
            }

            if (record.Input is not null && record.RouteKey is not null)
            {
                input = record.Input;
                break;
            }
        }

        context.Output = await ExecuteCoreAsync(input, context);

        context.AddExecutionRecord(GetType().Name, startedAt, DateTime.UtcNow, input, output: context.Output);
    }

    protected abstract Task<T> ExecuteCoreAsync(object? input, StepExecutionPlanContext context);
}

public class RootRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, StepExecutionPlanContext context)
    {
        string? routeKey = null;

        if (input.Contains("hola", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "fallback";
        }
        else if (input.Contains("soporte", StringComparison.OrdinalIgnoreCase))
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
    protected override Task<string> ExecuteCoreAsync(object? input, StepExecutionPlanContext context)
    {
        return Task.FromResult("¿Qué tal?");
    }
}

public class SupportRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, StepExecutionPlanContext context)
    {
        return Task.FromResult<string?>("nivel-1");
    }
}

public class SupportStep : ActionStepBase<string>
{
    protected override Task<string> ExecuteCoreAsync(object? input, StepExecutionPlanContext context)
    {
        return Task.FromResult("Hola, ¿en qué puedo ayudarte con soporte?");
    }
}

public class FirstLevelSupportStep : ActionStepBase<string>
{
    protected override Task<string> ExecuteCoreAsync(object? input, StepExecutionPlanContext context)
    {
        return Task.FromResult("Hola, ¿en qué puedo ayudarte con el soporte de primer nivel?");
    }
}

public class BusinessRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, StepExecutionPlanContext context)
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
    protected override Task<string> ExecuteCoreAsync(object? input, StepExecutionPlanContext context)
    {
        return Task.FromResult("Hola, ¿en qué puedo ayudarte con in-season?");
    }
}

public class PreSeasonStep : ActionStepBase<string>
{
    protected override Task<string> ExecuteCoreAsync(object? input, StepExecutionPlanContext context)
    {
        return Task.FromResult("Hola, ¿en qué puedo ayudarte con pre-season?");
    }
}

public interface IFluxify { }