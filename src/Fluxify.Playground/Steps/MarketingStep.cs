using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using System.ComponentModel;

namespace Fluxify.Playground.Steps;

public class MarketingCampaign
{
    public required string CampaignName { get; init; }
    public required string Platform { get; init; } // Google, LinkedIn, Facebook, etc.
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal Budget { get; init; }
    public decimal ActualCost { get; init; }
    public int Impressions { get; init; }
    public int Clicks { get; init; }
    public int Conversions { get; init; }
    public decimal ROI { get; init; }
    public required string Status { get; init; } // Active, Paused, Completed
}

public class MarketingPlugin
{
    private static readonly Random _random = new();

    [KernelFunction("get_marketing_campaigns")]
    [Description("Returns all marketing campaigns linked to the current user account.")]
    public static IEnumerable<MarketingCampaign> GetMarketingCampaigns()
    {
        var platforms = new[] { "Google Ads", "LinkedIn", "Facebook", "Instagram", "TikTok", "Twitter", "Email", "YouTube" };
        var statuses = new[] { "Active", "Paused", "Completed" };
        var campaigns = new List<MarketingCampaign>();

        for (int i = 1; i <= 10; i++)
        {
            var startDate = DateTime.Now.AddDays(-_random.Next(10, 180));
            var endDate = startDate.AddDays(_random.Next(30, 120));
            var budget = Math.Round(_random.Next(1000, 10000) + (decimal)_random.NextDouble(), 2);
            var actualCost = Math.Round(budget * (decimal)(_random.NextDouble() * 0.5 + 0.5), 2); // 50-100% of budget
            var impressions = _random.Next(10000, 1000000);
            var clicks = (int)(impressions * (_random.NextDouble() * 0.05 + 0.01)); // 1-6% CTR
            var conversions = (int)(clicks * (_random.NextDouble() * 0.1 + 0.01)); // 1-11% conversion rate
            var roi = Math.Round((decimal)(_random.NextDouble() * 4 + 0.5), 2); // 0.5-4.5x ROI

            var campaign = new MarketingCampaign
            {
                CampaignName = $"Campaign {i} - {DateTime.Now.Year}",
                Platform = platforms[_random.Next(platforms.Length)],
                StartDate = startDate,
                EndDate = endDate,
                Budget = budget,
                ActualCost = actualCost,
                Impressions = impressions,
                Clicks = clicks,
                Conversions = conversions,
                ROI = roi,
                Status = startDate > DateTime.Now ? "Scheduled" :
                         endDate < DateTime.Now ? "Completed" :
                         statuses[_random.Next(statuses.Length)]
            };

            campaigns.Add(campaign);
        }

        return campaigns;
    }
}

public class MarketingStep : ActionStepBase<string>
{
    private readonly Kernel _kernel;

    public MarketingStep(Kernel kernel)
    {
        _kernel = kernel;
        _kernel.Plugins.AddFromType<MarketingPlugin>();
    }

    protected override async Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {        
        var text = await EmbeddedResourceLoader.LoadAsync("Marketing.yaml", cancellationToken: cancellationToken);
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
