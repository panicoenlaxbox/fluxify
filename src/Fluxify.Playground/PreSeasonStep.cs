namespace Fluxify.Playground;

public class PreSeasonStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context)
    {
        return Task.FromResult<string?>("Hi, how can I help you with pre-season?");
    }
}
