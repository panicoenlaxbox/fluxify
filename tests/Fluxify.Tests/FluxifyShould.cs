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
                StepName = "RootRouterStep", Input = (string?)"hi", Output = (string?)null,
                RouteKey = (string?)"fallback"
            },
            new
            {
                StepName = "FallbackStep", Input = (string?)"hi", Output = (string?)"How are you?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
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
                StepName = "RootRouterStep", Input = (string?)"in-season", Output = (string?)null,
                RouteKey = (string?)"business"
            },
            new
            {
                StepName = "BusinessRouterStep", Input = (string?)"in-season", Output = (string?)null,
                RouteKey = (string?)"in-season"
            },
            new
            {
                StepName = "InSeasonStep", Input = (string?)"in-season",
                Output = (string?)"Hi, how can I help you with in-season?", RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task route_then_execute_and_execute()
    {
        var plan = new StepExecutionPlan
        {
            Root = new RootRouterStep()
        };
        var supportStep = new SupportStep();
        var firstLevelStep = new FirstLevelSupportStep();
        plan.ChildrenMap[(RouterStepBase)plan.Root] = new Dictionary<string, IStep>
        {
            { "support", supportStep }
        };
        plan.NextMap[supportStep] = firstLevelStep;
        var context = new StepExecutionPlanContext("support");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("Hi, how can I help you with level-1 support?");
        var expected = new[]
        {
            new
            {
                StepName = "RootRouterStep", Input = (string?)"support", Output = (string?)null,
                RouteKey = (string?)"support"
            },
            new
            {
                StepName = "SupportStep", Input = (string?)"support",
                Output = (string?)"Hi, how can I help you with support?", RouteKey = (string?)null
            },
            new
            {
                StepName = "FirstLevelSupportStep", Input = (string?)"Hi, how can I help you with support?",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
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
                StepName = "FirstLevelSupportStep", Input = (string?)"hi",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task execute_then_execute()
    {
        var supportStep = new SupportStep();
        var plan = new StepExecutionPlan
        {
            Root = supportStep
        };
        var firstLevelStep = new FirstLevelSupportStep();
        plan.NextMap[supportStep] = firstLevelStep;
        var context = new StepExecutionPlanContext("hi");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("Hi, how can I help you with level-1 support?");
        var expected = new[]
        {
            new
            {
                StepName = "SupportStep", Input = (string?)"hi",
                Output = (string?)"Hi, how can I help you with support?", RouteKey = (string?)null
            },
            new
            {
                StepName = "FirstLevelSupportStep", Input = (string?)"Hi, how can I help you with support?",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task route_then_execute_then_route_then_execute()
    {
        var plan = new StepExecutionPlan
        {
            Root = new RootRouterStep()
        };

        var supportStep = new SupportStep();
        var supportRouter = new SupportRouterStep();
        var firstLevelStep = new FirstLevelSupportStep();
        plan.ChildrenMap[(RouterStepBase)plan.Root] = new Dictionary<string, IStep> { { "support", supportStep } };
        plan.NextMap[supportStep] = supportRouter;
        plan.ChildrenMap[supportRouter] = new Dictionary<string, IStep> { { "level-1", firstLevelStep } };

        var context = new StepExecutionPlanContext("level-1 support");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("Hi, how can I help you with level-1 support?");
        var expected = new[]
        {
            new
            {
                StepName = "RootRouterStep", Input = (string?)"level-1 support", Output = (string?)null,
                RouteKey = (string?)"support"
            },
            new
            {
                StepName = "SupportStep", Input = (string?)"level-1 support",
                Output = (string?)"Hi, how can I help you with support?", RouteKey = (string?)null
            },
            new
            {
                StepName = "SupportRouterStep", Input = (string?)"Hi, how can I help you with support?",
                Output = (string?)null, RouteKey = (string?)"level-1"
            },
            new
            {
                StepName = "FirstLevelSupportStep", Input = (string?)"Hi, how can I help you with support?",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task execute_then_route_then_execute()
    {
        var supportStep = new SupportStep();
        var supportRouter = new SupportRouterStep();
        var firstLevelSupportStep = new FirstLevelSupportStep();
        var plan = new StepExecutionPlan
        {
            Root = supportStep,
            NextMap =
            {
                [supportStep] = supportRouter
            },
            ChildrenMap =
            {
                [supportRouter] = new Dictionary<string, IStep> { { "level-1", firstLevelSupportStep } }
            }
        };
        var context = new StepExecutionPlanContext("I need level-1 support");
        var runner = new StepExecutionPlanRunner();

        await runner.ExecuteAsync(context, plan);

        context.Output.ShouldBe("Hi, how can I help you with level-1 support?");
        var expected = new[]
        {
            new
            {
                StepName = "SupportStep", Input = (string?)"I need level-1 support",
                Output = (string?)"Hi, how can I help you with support?", RouteKey = (string?)null
            },
            new
            {
                StepName = "SupportRouterStep", Input = (string?)"Hi, how can I help you with support?",
                Output = (string?)null, RouteKey = (string?)"level-1"
            },
            new
            {
                StepName = "FirstLevelSupportStep", Input = (string?)"Hi, how can I help you with support?",
                Output = (string?)"Hi, how can I help you with level-1 support?",
                RouteKey = (string?)null
            }
        };
        var actual = context.ExecutionRecords.Select(st => new
        { st.StepName, Input = (string?)st.Input, Output = (string?)st.Output, st.RouteKey });
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
                       "RouteKey": "support",
                       "Children": [
                           {
                               "ServiceKey": "FirstLevelSupportStep"
                           }
                       ]
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