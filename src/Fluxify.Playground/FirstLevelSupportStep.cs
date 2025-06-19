namespace Fluxify.Playground;

public class FirstLevelSupportStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with level-1 support?");
    }
}
