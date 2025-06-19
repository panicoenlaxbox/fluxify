namespace Fluxify.Tests;

public class FakeRootRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        string? routeKey = null;

        if (input.Contains("hi", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "fallback";
        }
        else if (input.Contains("support", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "support";
        }
        else if (input.Contains("in-season", StringComparison.OrdinalIgnoreCase) ||
                 input.Contains("pre-season", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "business";
        }

        return Task.FromResult(routeKey);
    }
}

public class FakeFallbackStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("How are you?");
    }
}

public class FakeSupportRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("level-1");
    }
}

public class FakeSupportStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("Hi, how can I help you with support?");
    }
}

public class FakeFirstLevelSupportStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("Hi, how can I help you with level-1 support?");
    }
}

public class FakeBusinessRouterStep : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        string? routeKey = null;

        if (input.Contains("in-season", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "in-season";
        }
        else if (input.Contains("pre-season", StringComparison.OrdinalIgnoreCase))
        {
            routeKey = "pre-season";
        }

        return Task.FromResult(routeKey);
    }
}

public class FakeInSeasonStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("Hi, how can I help you with in-season?");
    }
}

public class FakePreSeasonStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>("Hi, how can I help you with pre-season?");
    }
}

public class FakeRouterStepWithoutRouteKey : RouterStepBase
{
    protected override Task<string?> GetRouteKeyAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}

public class InvalidInputTypeActionStep : ActionStepBase<string>
{
    protected override Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}