using Shouldly;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxify.Tests;

public class FluxifyFailuresShould
{

    [Fact]
    public async Task fail_when_router_has_no_children()
    {
        var router = new FakeRootRouterStep();
        var plan = new ExecutionPlan { Root = router };
        var context = new ExecutionPlanContext("hi");
        var runner = new ExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe($"Router {router.GetType().Name} has no registered children.");
    }

    [Fact]
    public async Task fail_when_route_key_not_found_in_children()
    {
        var router = new FakeRootRouterStep();
        var plan = new ExecutionPlan
        {
            Root = router,
            Children =
            {
                [router] = new Dictionary<string, IStep> { { "fallback", new FakeFallbackStep() } }
            }
        };
        var context = new ExecutionPlanContext("support");
        var runner = new ExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe($"Route key 'support' not found for router '{router.GetType().Name}'.");
    }

    [Fact]
    public void fail_when_context_output_type_is_invalid()
    {
        var context = new ExecutionPlanContext("hi")
        {
            Output = 1
        };

        var ex = Should.Throw<InvalidCastException>(context.GetOutput<string>);

        ex.Message.ShouldBe("Expected value of type 'String', but was 'Int32'.");
    }

    [Fact]
    public async Task fail_when_router_cannot_determine_route_key()
    {
        var router = new FakeRouterStepWithoutRouteKey();
        var plan = new ExecutionPlan { Root = router };
        var context = new ExecutionPlanContext("hi");
        var runner = new ExecutionPlanRunner();

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => runner.ExecuteAsync(context, plan));

        ex.Message.ShouldBe($"{router.GetType().Name} could not determine a valid route key.");
    }

    [Fact]
    public async Task fail_when_loading_from_json_and_action_step_has_children()
    {
        const string json = """
            {
                "ServiceKey": "FakeFallbackStep",
                "Children": [
                    {
                        "ServiceKey": "SupportStep",
                        "RouteKey": "support"
                    }
                ]
            }
            """;

        var services = new ServiceCollection();
        services.AddSteps<FluxifyFailuresShould>();
        await using var serviceProvider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => JsonExecutionPlanLoader.Load(json, serviceProvider));

        ex.Message.ShouldBe("Action step FakeFallbackStep cannot have children");
    }
    
    [Fact]
    public async Task fail_when_loading_from_json_and_router_step_is_missing_children()
    {
        const string json = """
            {
                "ServiceKey": "FakeRootRouterStep"
            }
            """;

        var services = new ServiceCollection();
        services.AddSteps<FluxifyFailuresShould>();
        await using var serviceProvider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => JsonExecutionPlanLoader.Load(json, serviceProvider));

        ex.Message.ShouldBe("Router step FakeRootRouterStep is missing children");
    }
    
    [Fact]
    public async Task fail_when_loading_from_json_and_child_is_missing_route_key()
    {
        const string json = """
            {
                "ServiceKey": "FakeRootRouterStep",
                "Children": [
                    {
                        "ServiceKey": "FakeFallbackStep"
                    }
                ]
            }
            """;

        var services = new ServiceCollection();
        services.AddSteps<FluxifyFailuresShould>();
        await using var serviceProvider = services.BuildServiceProvider();

        var ex = Should.Throw<InvalidOperationException>(() => JsonExecutionPlanLoader.Load(json, serviceProvider));

        ex.Message.ShouldBe("Child FakeFallbackStep missing RouteKey for parent FakeRootRouterStep");
    }
}
