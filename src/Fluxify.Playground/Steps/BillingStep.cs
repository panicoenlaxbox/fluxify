using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.ComponentModel;

namespace Fluxify.Playground.Steps;

public class Invoice
{
    public required string InvoiceNumber { get; init; }
    public decimal Amount { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public bool IsPaid { get; init; }    
    public required string Description { get; init; }
}

public class InvoicesPlugin
{
    private static readonly Random _random = new();

    [KernelFunction("get_invoices")]
    [Description("Returns all invoices linked to the current user account.")]
    public static IEnumerable<Invoice> GetInvoices()
    {
        var invoices = new List<Invoice>();

        for (int i = 1; i <= 10; i++)
        {
            var issueDate = DateTime.Now.AddDays(-_random.Next(1, 60));
            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.Now.Year}-{1000 + i}",
                Amount = Math.Round(_random.Next(100, 1000) + (decimal)_random.NextDouble(), 2),
                IssueDate = issueDate,
                DueDate = issueDate.AddDays(30),
                IsPaid = _random.Next(0, 2) == 1,
                Description = $"Services - {DateTime.Now.AddMonths(-_random.Next(1, 3)):MMMM yyyy}"
            };
            invoices.Add(invoice);
        }

        return invoices;
    }
}

public class BillingStep : ActionStepBase<string>
{
    private readonly Kernel _kernel;

    public BillingStep(Kernel kernel)
    {
        _kernel = kernel;
        _kernel.Plugins.AddFromType<InvoicesPlugin>();
    }

    protected override async Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(Path.Combine("Steps", "Prompts", "Billing.yaml"), cancellationToken);
        var function = _kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments(new PromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()            
        })
        {
            ["history"] = context.GetHistoryForPromptTemplate()
        };
        var functionResult = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();
    }
}
