# Fluxify

**Fluxify** is a lightweight C# library designed to simplify the creation of execution workflows.

It currently supports any combination of two step types: **Router** steps and **Action** steps. This flexible architecture allows you to build dynamic and modular execution pipelines tailored to a variety of use cases.

The motivation behind Fluxify is to implement the **routing pattern** and **prompt chaining** concepts described in [Anthropic's article on building effective agents](https://www.anthropic.com/engineering/building-effective-agents).

To build an execution plan, you can either load it from a JSON configuration or construct it manually in code.

To see an example of loading from JSON, check the test case named [load_plan_from_json](tests/Fluxify.Tests/FluxifyShould.cs).

For examples of building plans manually, any other test case in the project is self-explanatory.
