namespace Shafiee.CLI.Reporting;

using System;
using Shafiee.SDK.Results;

public static class ConsoleResultReporter
{
    public static void PrintReport(ExecutionResult result)
    {
        Console.WriteLine();
        Console.WriteLine("========================================s");
        Console.WriteLine("           EXECUTION REPORT             ");
        Console.WriteLine("========================================");

        // ۱. نمایش خطاها و اخطارها به انگلیسی
        if (result.Diagnostics.Any())
        {
            Console.WriteLine("\n📌 Diagnostics & Messages:");
            foreach (var diag in result.Diagnostics)
            {
                switch (diag.Severity)
                {
                    case DiagnosticSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ❌ [Error] {diag.Message}");
                        break;
                    case DiagnosticSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⚠️ [Warning] {diag.Message}");
                        break;
                    case DiagnosticSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  ℹ️ [Info] {diag.Message}");
                        break;
                }
                Console.ResetColor();
            }
        }

        // ۲. نمایش آمار عملکرد به انگلیسی
        Console.WriteLine("\n📊 Performance Statistics:");
        Console.WriteLine($"  🟢 Created Artifacts: {result.Statistics.CreatedCount}");
        Console.WriteLine($"  ⏱️ Elapsed Time:      {result.Statistics.ElapsedTime.TotalMilliseconds:F0} ms");

        Console.WriteLine("========================================\n");
    }
}