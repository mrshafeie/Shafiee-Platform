namespace Shafiee.SDK.Results;

using Shafiee.SDK.Artifacts;

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public record Diagnostic(string Message, DiagnosticSeverity Severity = DiagnosticSeverity.Info);

public class ExecutionStatistics
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}

public class ExecutionResult
{
    public IReadOnlyCollection<Artifact> Artifacts { get; }
    public IReadOnlyCollection<Diagnostic> Diagnostics { get; }
    public ExecutionStatistics Statistics { get; }

    public ExecutionResult(
        IEnumerable<Artifact> artifacts,
        IEnumerable<Diagnostic> diagnostics,
        ExecutionStatistics statistics)
    {
        Artifacts = artifacts.ToList().AsReadOnly();
        Diagnostics = diagnostics.ToList().AsReadOnly();
        Statistics = statistics;
    }
}