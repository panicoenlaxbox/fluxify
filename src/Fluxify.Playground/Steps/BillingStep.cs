using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.ComponentModel;
using System.Net.Mime;

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

public class BillingPlugin([FromKeyedServices("SupportStep")] IStep supportStep)
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

    [KernelFunction("get_support")]
    [Description("Provides support information relevant to the user's issue based on their input within the conversation context.")]
    public async Task<string?> GetSupport(
        [Description("A brief summary of the user's question or issue in the context of the current conversation.")]
    string query,
        CancellationToken cancellationToken = default)
    {
        var context = new ExecutionPlanContext(query);
        await supportStep.ExecuteAsync(context, cancellationToken);
        return context.GetOutput<string?>();
    }
}

public class BillingStep : ActionStepBase<string>
{
    private readonly Kernel _kernel;
    private readonly IServiceProvider _serviceProvider;

    public BillingStep(Kernel kernel, IServiceProvider serviceProvider)
    {
        _kernel = kernel;
        _serviceProvider = serviceProvider;
    }

    protected override async Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        if (!_kernel.Plugins.Contains(nameof(BillingPlugin)))
        {            
            _kernel.Plugins.AddFromType<BillingPlugin>(nameof(BillingPlugin), _serviceProvider);
        }
        var text = await EmbeddedResourceLoader.LoadAsync("Billing.yaml", cancellationToken: cancellationToken);
        var function = _kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments(new PromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
        {
            ["history"] = context.GetHistoryForPrompt()
        };
        var functionResult = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();
    }
}
