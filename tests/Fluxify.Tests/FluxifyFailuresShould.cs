using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

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

    [Fact]
    public async Task fail_when_loading_from_json_and_action_step_has_children()
    {
        const string json = """
            {
                "ServiceKey": "FallbackStep",
                "Children": [
                    {
                        "ServiceKey": "SupportStep",
                        "RouteKey": "support"
                    }
                ]
            }
            """;

        var services = new ServiceCollection();
        services.AddSteps<IFluxify>();
        await using var serviceProvider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => JsonStepExecutionPlanLoader.Load(json, serviceProvider));

        ex.Message.ShouldBe("Action step FallbackStep cannot have children");
    }
    
    [Fact]
    public async Task fail_when_loading_from_json_and_router_step_is_missing_children()
    {
        const string json = """
            {
                "ServiceKey": "RootRouterStep"
            }
            """;

        var services = new ServiceCollection();
        services.AddSteps<IFluxify>();
        await using var serviceProvider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => JsonStepExecutionPlanLoader.Load(json, serviceProvider));

        ex.Message.ShouldBe("Router step RootRouterStep is missing children");
    }
    
    [Fact]
    public async Task fail_when_loading_from_json_and_child_is_missing_route_key()
    {
        const string json = """
            {
                "ServiceKey": "RootRouterStep",
                "Children": [
                    {
                        "ServiceKey": "FallbackStep"
                    }
                ]
            }
            """;

        var services = new ServiceCollection();
        services.AddSteps<IFluxify>();
        await using var serviceProvider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => JsonStepExecutionPlanLoader.Load(json, serviceProvider));

        ex.Message.ShouldBe("Child FallbackStep missing RouteKey for parent RootRouterStep");
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
