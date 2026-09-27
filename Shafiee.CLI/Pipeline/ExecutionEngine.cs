namespace Shafiee.CLI.Pipeline;

using System.Diagnostics;
using Shafiee.SDK.Commands;
using Shafiee.SDK.Pipeline;
using Shafiee.SDK.Results;

public class ExecutionEngine
{
    private readonly IEnumerable<IPipelineStep> _steps;

    public ExecutionEngine(IEnumerable<IPipelineStep> steps)
    {
        _steps = steps;
    }

    public async Task<ExecutionResult> RunAsync(UserCommand command, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = new PipelineContext
        {
            Command = command
        };

        foreach (var step in _steps)
        {
            if (!context.IsSuccess)
            {
                break;
            }

            try
            {
                await step.ExecuteAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                context.IsSuccess = false;
                // ترجمه پیام خطا به انگلیسی
                context.Diagnostics.Add(new Diagnostic($"Error executing step {step.GetType().Name}: {ex.Message}", DiagnosticSeverity.Error));
            }
        }

        stopwatch.Stop();

        context.Statistics.ElapsedTime = stopwatch.Elapsed;
        context.Statistics.CreatedCount = context.Artifacts.Count;

        return new ExecutionResult(context.Artifacts, context.Diagnostics, context.Statistics);
    }
}