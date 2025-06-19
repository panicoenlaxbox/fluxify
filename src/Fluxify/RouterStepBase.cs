namespace Fluxify;

public abstract class RouterStepBase : IRouterStep
{
    public async Task ExecuteAsync(ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        context.LastRouteKey = await GetRouteKeyAsync(context.Input, context, cancellationToken) ??
                               throw new InvalidOperationException(
                                   $"{GetType().Name} could not determine a valid route key.");

        context.AddExecutionRecord(GetType().Name, startedAt, DateTime.UtcNow, routeKey: context.LastRouteKey);
    }

    protected abstract Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default);
}