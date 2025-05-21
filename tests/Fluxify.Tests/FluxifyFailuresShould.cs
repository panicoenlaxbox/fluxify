using Shouldly;

namespace Fluxify.Tests;

public class FluxifyFailuresShould
{

    [Fact]
    public async Task fail_when_router_has_no_children()
    {
        var router = new RootRouterStep();
        var plan = new StepExecutionPlan { Root = router };
        var context = new StepExecutionPlanContext("hi");
        var runner = new StepExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe($"Router {router.GetType().Name} has no registered children.");
    }

    [Fact]
    public async Task fail_when_route_key_not_found_in_children()
    {
        var router = new RootRouterStep();
        var plan = new StepExecutionPlan
        {
            Root = router,
            ChildrenMap =
            {
                [router] = new Dictionary<string, IStep> { { "fallback", new FallbackStep() } }
            }
        };
        var context = new StepExecutionPlanContext("support");
        var runner = new StepExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe($"Route key 'support' not found for router '{router.GetType().Name}'.");
    }

    [Fact]
    public void fail_when_context_input_type_is_invalid()
    {
        var context = new StepExecutionPlanContext(1);

        var ex = Should.Throw<InvalidCastException>(context.GetInput<string>);

        ex.Message.ShouldBe("Expected value of type 'String', but was 'Int32'.");
    }

    [Fact]
    public void fail_when_context_output_type_is_invalid()
    {
        var context = new StepExecutionPlanContext("hi")
        {
            Output = 1
        };

        var ex = Should.Throw<InvalidCastException>(context.GetOutput<string>);

        ex.Message.ShouldBe("Expected value of type 'String', but was 'Int32'.");
    }

    [Fact]
    public async Task fail_when_router_cannot_determine_route_key()
    {
        var router = new RouterStepWithoutRouteKey();
        var plan = new StepExecutionPlan { Root = router };
        var context = new StepExecutionPlanContext("hi");
        var runner = new StepExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe($"{router.GetType().Name} could not determine a valid route key.");
    }

    [Fact]
    public async Task fail_when_input_type_does_not_match_action_step_expected_type()
    {
        var step = new InvalidInputTypeActionStep();
        var plan = new StepExecutionPlan { Root = step };
        var context = new StepExecutionPlanContext(123);
        var runner = new StepExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe("Input is not of type String.");
    }

    private class RouterStepWithoutRouteKey : RouterStepBase
    {
        protected override Task<string?> GetRouteKeyAsync(string input, StepExecutionPlanContext context) =>
            Task.FromResult<string?>(null);
    }

    private class InvalidInputTypeActionStep : ActionStepBase<string, string>
    {
        protected override Task<string?> ExecuteCoreAsync(string? input, StepExecutionPlanContext context) =>
            Task.FromResult<string?>(null);
    }
}
