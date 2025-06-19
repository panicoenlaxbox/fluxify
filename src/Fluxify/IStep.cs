namespace Fluxify;

public interface IStep
{
    Task ExecuteAsync(ExecutionPlanContext context, CancellationToken cancellationToken = default);
}