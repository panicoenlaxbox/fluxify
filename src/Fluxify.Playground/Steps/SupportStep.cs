namespace Fluxify.Playground.Steps;

public class SupportStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with support?");
    }
}
