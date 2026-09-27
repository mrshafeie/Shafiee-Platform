namespace Shafiee.SDK.Pipeline;

public interface IPipelineStep
{
    Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default);
}