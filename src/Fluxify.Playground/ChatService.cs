using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Fluxify.Playground;

internal class ChatService(IServiceProvider serviceProvider)
{
    private static string GetUserInput()
    {
        Console.Write("User > ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Environment.Exit(0);
        }
        return input;
    }

    public async Task ExecuteAsync(string json, CancellationToken cancellationToken = default)
    {
        var chatHistoryReducer = new ChatHistorySummarizationReducer(serviceProvider.GetRequiredService<IChatCompletionService>(), targetCount: 2, thresholdCount: 4);

        var input = GetUserInput();

        var plan = JsonExecutionPlanLoader.Load(json, serviceProvider);
        var context = new ExecutionPlanContext(input);
        var runner = new ExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan, cancellationToken);

        while (true)
        {
            Console.Write("Assistant > ");
            Console.WriteLine(context.GetOutput<string>());

            context.History = [.. await chatHistoryReducer.ReduceAsync(context.History, cancellationToken) ?? context.History];

            input = GetUserInput();

            context.Input = input;

            await runner.ExecuteAsync(context, plan, cancellationToken);
        }
    }
}
