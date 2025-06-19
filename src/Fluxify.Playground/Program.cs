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
