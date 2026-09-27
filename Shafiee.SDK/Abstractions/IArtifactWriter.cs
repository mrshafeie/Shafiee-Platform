namespace Shafiee.SDK.Abstractions;

using Shafiee.SDK.Artifacts;

public interface IArtifactWriter
{
    Task WriteAsync(IEnumerable<Artifact> artifacts, CancellationToken cancellationToken = default);
}