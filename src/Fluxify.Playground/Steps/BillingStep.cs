using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Fluxify.Playground.Steps;

public class BillingStep : ActionStepBase<string>
{
    private readonly Kernel _kernel;

    public BillingStep(Kernel kernel)
    {
        _kernel = kernel;
    }

    protected override async Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(Path.Combine("Steps", "Prompts", "Billing.yaml"), cancellationToken);
        var function = _kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments
        {
            ["history"] = context.GetHistoryForPromptTemplate()
        };
        var functionResult = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();
    }
}
