# Fluxify

**Fluxify** is a lightweight C# library designed to simplify the creation of execution workflows.

It supports two distinct step types:

- **Router** steps: Determine the path to follow based on input, routing to the next appropriate step.
- **Action** steps: Terminal steps that perform operations and produce output.

This architecture allows you to build workflows where multiple routers can direct execution through a decision tree until reaching a terminal action step. Every execution path ends with exactly one action step that produces the final result.

The motivation behind Fluxify is to implement the **routing pattern** concepts described in [Anthropic's article on building effective agents](https://www.anthropic.com/engineering/building-effective-agents).

## Building an execution plan

You can build an execution plan in two ways:

1. **Load from JSON configuration**: Define your routing tree with a structured JSON definition.
2. **Construct manually in code**: Create and connect router and action steps programmatically.

### Examples

To see an example of loading from JSON, check the test case named [load_plan_from_json](tests/Fluxify.Tests/FluxifyShould.cs).

For examples of building plans manually and different execution patterns, see the test cases in [FluxifyShould.cs](tests/Fluxify.Tests/FluxifyShould.cs).
