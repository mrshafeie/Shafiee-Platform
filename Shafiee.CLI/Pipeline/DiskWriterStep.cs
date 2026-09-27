using Shafiee.CLI.Writers;
using Shafiee.SDK.Pipeline;

namespace Shafiee.CLI.Pipeline;
public class DiskWriterStep : IPipelineStep
{
    private readonly DiskArtifactWriter _writer;

    public DiskWriterStep(DiskArtifactWriter writer)
    {
        _writer = writer;
    }

    public async Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (context.Artifacts.Count > 0)
        {
            await _writer.WriteAsync(context.Artifacts, cancellationToken);
        }
    }
}