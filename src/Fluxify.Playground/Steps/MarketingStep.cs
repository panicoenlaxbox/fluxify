namespace Fluxify.Playground.Steps;

public class MarketingStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi! How can I assist you with marketing?");
    }
}
