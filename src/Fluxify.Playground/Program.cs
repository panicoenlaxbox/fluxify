using Fluxify;
using Fluxify.Playground;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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

services.AddHttpHandlers();

var deploymentName = configuration["AzureOpenAI:DeploymentName"]!;
var endpoint = configuration["AzureOpenAI:Endpoint"]!;
var apiKey = configuration["AzureOpenAI:ApiKey"]!;
services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
services.AddKernel();
services.AddTransient<ChatService>();

await using var serviceProvider = services.BuildServiceProvider();
LoggerBase.LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

const string json = """
                    {
                        "ServiceKey": "RootRouterStep",
                        "Children": [
                            {
                                "ServiceKey": "SupportStep",
                                "RouteKey": "support"
                            },        
                            {
                                "ServiceKey": "BusinessRouterStep",
                                "RouteKey": "business",
                                "Children": [
                                    {
                                        "ServiceKey": "MarketingStep",
                                        "RouteKey": "marketing"
                                    },
                                    {
                                        "ServiceKey": "BillingStep",
                                        "RouteKey": "billing"
                                    }
                                ]
                            }
                        ]
                    }
                    """;

var chatService = serviceProvider.GetRequiredService<ChatService>();

await chatService.RunAsync(json);