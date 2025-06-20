using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Fluxify.Playground.Steps;

public class RootRouterStep(Kernel kernel) : RouterStepBase
{
    protected override async Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        var text = await EmbeddedResourceLoader.LoadAsync("RootRouter.yaml", cancellationToken: cancellationToken);
        var function = kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments
        {
            ["previous_routing"] = context.GetLatestRouteByRouter(nameof(RootRouterStep)),
            ["history"] = context.GetHistoryForPrompt()
        };
        var functionResult = await kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();        
    }
}
