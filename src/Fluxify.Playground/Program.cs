#pragma warning disable SKEXP0010

using AgenticPatterns;
using Fluxify;
using Fluxify.Playground;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Qdrant.Client;

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

var deploymentName = configuration["AzureOpenAI:ChatCompletion:DeploymentName"]!;
var endpoint = configuration["AzureOpenAI:ChatCompletion:Endpoint"]!;
var apiKey = configuration["AzureOpenAI:ChatCompletion:ApiKey"]!;
services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
deploymentName = configuration["AzureOpenAI:EmbeddingGenerator:DeploymentName"]!;
endpoint = configuration["AzureOpenAI:EmbeddingGenerator:Endpoint"]!;
apiKey = configuration["AzureOpenAI:EmbeddingGenerator:ApiKey"]!;
services.AddAzureOpenAIEmbeddingGenerator(
    deploymentName: deploymentName,
    endpoint: endpoint,
    apiKey: apiKey);
services.AddKernel();
services.AddSingleton<IPromptRenderFilter, HtmlCommentStripperFilter>();

services.AddVectorStoreTextSearch<Document>();
services.AddQdrantVectorStore(sp => sp.GetRequiredService<QdrantClient>());
services.AddTransient(sp => new QdrantClient("localhost"));
services.AddQdrantCollection<ulong, Document>("skdocuments");

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

await chatService.ExecuteAsync(json);