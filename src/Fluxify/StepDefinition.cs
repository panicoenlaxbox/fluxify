namespace Fluxify;

public class StepDefinition
{
    public required string ServiceKey { get; init; }
    public string? RouteKey { get; init; }
    public IEnumerable<StepDefinition>? Children { get; init; }
}