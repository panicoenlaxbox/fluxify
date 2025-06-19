namespace Fluxify;

public abstract class ActionStepBase<TOutput> : IActionStep
{
    public async Task ExecuteAsync(ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        context.Output = await ExecuteCoreAsync(context.Input, context, cancellationToken);

        context.AddExecutionRecord(GetType().Name, startedAt, DateTime.UtcNow, output: context.Output);
    }

    protected abstract Task<TOutput?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default);
}