using Fluxify;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .Build();

var services = new ServiceCollection();

services
    .AddLogging(configure => configure
        .AddConfiguration(configuration.GetSection("Logging"))
        .AddConsole());

services.AddSteps<Program>();

var deploymentName = configuration["AzureOpenAI:DeploymentName"]!;
var endpoint = configuration["AzureOpenAI:Endpoint"]!;
var apiKey = configuration["AzureOpenAI:ApiKey"]!;
services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
services.AddKernel();

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
