using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Fluxify.Playground.Steps;

public class BusinessRouterStep(Kernel kernel) : RouterStepBase
{
    protected override async Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {        
        var text = await File.ReadAllTextAsync(Path.Combine("Steps", "BusinessRouter.yaml"), cancellationToken);
        var function = kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments
        {
            ["previous_routing"] = context.GetLatestRouteByRouter(nameof(BusinessRouterStep)),
            ["history"] = context.GetHistoryForPrompt()
        };
        var functionResult = await kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();        
    }
}