using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fluxify.Tests;

public class FluxifyShould
{
    [Fact]
    public async Task route_then_execute()
    {
        var plan = new ExecutionPlan
        {
            Root = new FakeRootRouterStep()
        };
        plan.Children[(RouterStepBase)plan.Root] = new Dictionary<string, IStep>
        {
            { "fallback", new FakeFallbackStep() }
        };
        var context = new ExecutionPlanContext("hi");
        var runner = new ExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan, TestContext.Current.CancellationToken);

        context.Output.ShouldBe("How are you?");
        var expected = new[]
        {
            new
            {
                StepName = "FakeRootRouterStep",
                Input = "hi",
                Output = (string?)null,
                RouteKey = (string?)"fallback"
            },
            new
            {
                StepName = "FakeFallbackStep",
                Input = "hi",
                Output = (string?)"How are you?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st =>
            new
            {
                st.StepName,
                st.Input,
                Output = (string?)st.Output,
                st.RouteKey
            });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task route_and_route_then_execute()
    {
        var plan = new ExecutionPlan
        {
            Root = new FakeRootRouterStep()
        };
        var businessRouter = new FakeBusinessRouterStep();
        var inSeasonStep = new FakeInSeasonStep();
        plan.Children[(RouterStepBase)plan.Root] = new Dictionary<string, IStep>
        {
            { "business", businessRouter }
        };
        plan.Children[businessRouter] = new Dictionary<string, IStep>
        {
            { "in-season", inSeasonStep }
        };
        var context = new ExecutionPlanContext("in-season");
        var runner = new ExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan, TestContext.Current.CancellationToken);

        context.Output.ShouldBe("Hi, how can I help you with in-season?");
        var expected = new[]
        {
            new
            {
                StepName = "FakeRootRouterStep", 
                Input = "in-season", 
                Output = (string?)null,
                RouteKey = (string?)"business"
            },
            new
            {
                StepName = "FakeBusinessRouterStep", 
                Input = "in-season", 
                Output = (string?)null,
                RouteKey = (string?)"in-season"
            },
            new
            {
                StepName = "FakeInSeasonStep", 
                Input = "in-season",
                Output = (string?)"Hi, how can I help you with in-season?", 
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st =>
            new
            {
                st.StepName,
                st.Input,
                Output = (string?)st.Output,
                st.RouteKey
            });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task execute()
    {
        var plan = new ExecutionPlan
        {
            Root = new FakeFirstLevelSupportStep()
        };
        var context = new ExecutionPlanContext("hi");
        var runner = new ExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan, TestContext.Current.CancellationToken);

        context.Output.ShouldBe("Hi, how can I help you with level-1 support?");
        var expected = new[]
        {
            new
            {
                StepName = "FakeFirstLevelSupportStep", 
                Input = "hi",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st =>
            new
            {
                st.StepName,
                st.Input,
                Output = (string?)st.Output,
                st.RouteKey
            });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task load_plan_from_json()
    {
        const string json = """
           {
               "ServiceKey": "FakeRootRouterStep",
               "Children": [
                   {
                       "ServiceKey": "FakeFallbackStep",
                       "RouteKey": "fallback"
                   },
                   {
                       "ServiceKey": "FakeSupportStep",
                       "RouteKey": "support"
                   },
                   {
                       "ServiceKey": "FakeBusinessRouterStep",
                       "RouteKey": "business",
                       "Children": [
                           {
                               "ServiceKey": "FakeInSeasonStep",
                               "RouteKey": "in-season"
                           },
                           {
                               "ServiceKey": "FakePreSeasonStep",
                               "RouteKey": "pre-season"
                           }
                       ]
                   }
               ]
           }
           """;
        var services = new ServiceCollection();
        services.AddSteps<FluxifyShould>();
        await using var serviceProvider = services.BuildServiceProvider();

        var plan = JsonExecutionPlanLoader.Load(json, serviceProvider);

        plan.AsJson().ShouldBe(json);
    }
}