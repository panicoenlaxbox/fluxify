using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fluxify.Tests;

public class FluxifyShould
{
    [Fact]
    public async Task route_then_execute()
    {
        var plan = new StepExecutionPlan
        {
            Root = new RootRouterStep()
        };
        plan.ChildrenMap[(RouterStepBase)plan.Root] = new Dictionary<string, IStep>
        {
            { "fallback", new FallbackStep() }
        };
        var context = new StepExecutionPlanContext("hi");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("How are you?");
        var expected = new[]
        {
            new
            {
                StepName = "RootRouterStep",
                Input = (string?)"hi",
                Output = (string?)null,
                RouteKey = (string?)"fallback"
            },
            new
            {
                StepName = "FallbackStep",
                Input = (string?)"hi",
                Output = (string?)"How are you?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st =>
            new
            {
                st.StepName,
                Input = (string?)st.Input,
                Output = (string?)st.Output,
                st.RouteKey
            });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task route_and_route_then_execute()
    {
        var plan = new StepExecutionPlan
        {
            Root = new RootRouterStep()
        };
        var businessRouter = new BusinessRouterStep();
        var inSeasonStep = new InSeasonStep();
        plan.ChildrenMap[(RouterStepBase)plan.Root] = new Dictionary<string, IStep>
        {
            { "business", businessRouter }
        };
        plan.ChildrenMap[businessRouter] = new Dictionary<string, IStep>
        {
            { "in-season", inSeasonStep }
        };
        var context = new StepExecutionPlanContext("in-season");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("Hi, how can I help you with in-season?");
        var expected = new[]
        {
            new
            {
                StepName = "RootRouterStep", 
                Input = (string?)"in-season", 
                Output = (string?)null,
                RouteKey = (string?)"business"
            },
            new
            {
                StepName = "BusinessRouterStep", 
                Input = (string?)"in-season", 
                Output = (string?)null,
                RouteKey = (string?)"in-season"
            },
            new
            {
                StepName = "InSeasonStep", 
                Input = (string?)"in-season",
                Output = (string?)"Hi, how can I help you with in-season?", 
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st =>
            new
            {
                st.StepName,
                Input = (string?)st.Input,
                Output = (string?)st.Output,
                st.RouteKey
            });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task execute()
    {
        var plan = new StepExecutionPlan
        {
            Root = new FirstLevelSupportStep()
        };
        var context = new StepExecutionPlanContext("hi");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("Hi, how can I help you with level-1 support?");
        var expected = new[]
        {
            new
            {
                StepName = "FirstLevelSupportStep", 
                Input = (string?)"hi",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st =>
            new
            {
                st.StepName,
                Input = (string?)st.Input,
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
               "ServiceKey": "RootRouterStep",
               "Children": [
                   {
                       "ServiceKey": "FallbackStep",
                       "RouteKey": "fallback"
                   },
                   {
                       "ServiceKey": "SupportStep",
                       "RouteKey": "support"
                   },
                   {
                       "ServiceKey": "BusinessRouterStep",
                       "RouteKey": "business",
                       "Children": [
                           {
                               "ServiceKey": "InSeasonStep",
                               "RouteKey": "in-season"
                           },
                           {
                               "ServiceKey": "PreSeasonStep",
                               "RouteKey": "pre-season"
                           }
                       ]
                   }
               ]
           }
           """;
        var services = new ServiceCollection();
        services.AddSteps<IFluxify>();
        await using var serviceProvider = services.BuildServiceProvider();

        var plan = JsonStepExecutionPlanLoader.Load(json, serviceProvider);

        plan.AsJson().ShouldBe(json);
    }
}