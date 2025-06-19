namespace Fluxify.Playground.Steps;

public class BusinessRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context)
    {
        string? routeKey = null;

        if (input.Contains("marketing", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "marketing";
        }
        else if (input.Contains("billing", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "billing";
        }

        return Task.FromResult(routeKey);
    }
}