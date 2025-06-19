namespace Fluxify.Playground;

public class FallbackStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("How are you?");
    }
}
