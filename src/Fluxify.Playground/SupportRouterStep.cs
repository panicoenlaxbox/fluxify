namespace Fluxify.Playground;

public class SupportRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("level-1");
    }
}
