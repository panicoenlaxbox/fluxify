namespace Fluxify.Playground.Steps;

public class RootRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context)
    {
        string? routeKey = null;

        if (input.Contains("support", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "support";
        }
        else if (input.Contains("marketing", StringComparison.OrdinalIgnoreCase) ||
                 input.Contains("billing", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "business";
        }

        return Task.FromResult(routeKey);
    }
}
