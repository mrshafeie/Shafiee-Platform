namespace Shafiee.SDK.Pipeline;

using Shafiee.SDK.Artifacts;
using Shafiee.SDK.Commands;
using Shafiee.SDK.Results;

public class PipelineContext
{
    public required UserCommand Command { get; init; }

    // مسیر ریشه یا جاری اجرای دستور
    public string BaseDirectory { get; set; } = Directory.GetCurrentDirectory();

    // وضعیت موفقیت‌آمیز بودن مرحله
    public bool IsSuccess { get; set; } = true;

    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();
    public List<Artifact> Artifacts { get; } = new();
    public List<Diagnostic> Diagnostics { get; } = new();
    public ExecutionStatistics Statistics { get; set; } = new();
}