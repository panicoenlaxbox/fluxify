namespace Fluxify.Playground.Steps;

public class BillingStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I assist you with your billing?");
    }
}
