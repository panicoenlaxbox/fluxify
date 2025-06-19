using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Fluxify.Playground.Steps;

public class SupportStep(Kernel kernel) : ActionStepBase<string>
{
    protected override async Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(Path.Combine("Steps", "Prompts", "Support.yaml"), cancellationToken);
        var function = kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments
        {
            ["history"] = context.GetHistoryForPromptTemplate()
        };
        var functionResult = await kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();        
    }
}