using Microsoft.Extensions.Logging;

namespace Fluxify;

public class ExecutionPlanRunner : LoggerBase
{
    public async Task ExecuteAsync(ExecutionPlanContext context, ExecutionPlan plan)
    {
        var currentStep = plan.Root;

        while (currentStep is not null)
        {
            if (currentStep is RouterStepBase routerStep)
            {
                Logger.LogDebug("Executing router step {StepName}", currentStep.GetType().Name);

                await currentStep.ExecuteAsync(context);

                var routeKey = context.LastRouteKey!;

                Logger.LogDebug("Router step {StepName} determined route key {RouteKey}", currentStep.GetType().Name,
                    routeKey);

                if (!plan.Children.TryGetValue(routerStep, out var childSteps))
                {
                    throw new InvalidOperationException(
                        $"Router {routerStep.GetType().Name} has no registered children.");
                }

                if (!childSteps.TryGetValue(routeKey, out var routedStep))
                {
                    throw new InvalidOperationException(
                        $"Route key '{routeKey}' not found for router '{routerStep.GetType().Name}'.");
                }

                currentStep = routedStep;
            }
            else
            {
                Logger.LogDebug("Executing action step {StepName}", currentStep.GetType().Name);

                await currentStep.ExecuteAsync(context);

                currentStep = null;
            }
        }
    }
}