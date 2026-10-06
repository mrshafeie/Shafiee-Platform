using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Shafiee.CLI.Pipeline;
using Shafiee.CLI.Reporting;
using Shafiee.CLI.Writers;
using Shafiee.Core.Parsers;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // 1. Parse incoming terminal commands using CommandParser
            var command = CommandParser.Parse(args);

            // 2. Resolve appropriate execution steps based on the command type (Router)
            var steps = PackageRouter.ResolveSteps(command).ToList();

            if (!steps.Any())
            {
                Console.WriteLine($"❌ [Error] Unsupported or invalid command: '{command.Verb} {command.Resource}'");
                ShowHelp();
                return;
            }

            // 3. Smart default path management for 'new' and 'generate' commands
            if (command.Verb.Equals("generate", StringComparison.OrdinalIgnoreCase) ||
                command.Verb.Equals("new", StringComparison.OrdinalIgnoreCase))
            {
                string outputRoot = command.GetOption("output") ?? command.GetOption("path");

                // If no explicit path was provided, smartly use the default desktop location
                if (string.IsNullOrEmpty(outputRoot))
                {
                    string solutionName = command.GetOption("solution") ?? command.Target;

                    if (command.Verb.Equals("generate") && command.Resource.Equals("crud") && string.IsNullOrEmpty(solutionName))
                    {
                        throw new ArgumentException("Please specify the solution or project name. Example: shafiee generate crud hop");
                    }

                    // If it's a generate command and solution isn't explicitly passed, use Target as solution name
                    if (command.Verb.Equals("generate") && string.IsNullOrEmpty(command.GetOption("solution")) && !string.IsNullOrEmpty(command.Target))
                    {
                        solutionName = command.Target;
                        command.Options["solution"] = solutionName; // Auto-assign as Solution option
                    }

                    string defaultDesktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");

                    if (!string.IsNullOrEmpty(solutionName))
                    {
                        outputRoot = Path.Combine(defaultDesktopPath, solutionName);
                    }
                    else
                    {
                        outputRoot = defaultDesktopPath;
                    }
                }

                var diskWriter = new DiskArtifactWriter();
                diskWriter.SetBaseDirectory(outputRoot);

                steps.Add(new DiskWriterStep(diskWriter));
            }

            // 4. Initialize ExecutionEngine and run the pipeline
            var engine = new ExecutionEngine(steps);
            var result = await engine.RunAsync(command);

            // 5. Print final report using the console reporter
            ConsoleResultReporter.PrintReport(result);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n❌ [Fatal Error]: {ex.Message}");
            Console.ResetColor();
            ShowHelp();
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("\n💡 Shafiee CLI Usage Guide:");
        Console.WriteLine("   1. Create a new solution:");
        Console.WriteLine("      dotnet run -- new solution hop --arch layered");
        Console.WriteLine("   2. Generate CRUD codes (fully automated):");
        Console.WriteLine("      dotnet run -- generate crud hop");
        Console.WriteLine("   3. Generate Project Shared codes (fully automated):");
        Console.WriteLine("      dotnet run -- shafiee generate shared");
        Console.WriteLine(new string('-', 50));
    }
}