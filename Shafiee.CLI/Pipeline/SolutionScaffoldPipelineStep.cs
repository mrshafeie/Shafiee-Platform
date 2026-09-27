namespace Shafiee.CLI.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shafiee.Architectures;
using Shafiee.SDK.Artifacts;
using Shafiee.SDK.Pipeline;
using Shafiee.SDK.Results;

public class SolutionScaffoldPipelineStep : IPipelineStep
{
    private static readonly List<string> SupportedArchitectures = new List<string> { "layered", "clean", "microservice" };

    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        string solutionName = context.Command.Target ?? "MySolution";
        string archName = context.Command.GetOption("arch") ?? context.Command.GetOption("architecture") ?? "layered";

        if (!SupportedArchitectures.Contains(archName.ToLower()))
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Invalid architecture: '{archName}'. Supported: {string.Join(", ", SupportedArchitectures)}", DiagnosticSeverity.Error));
            return Task.CompletedTask;
        }

        // تعیین مسیر خروجی (پيش‌فرض: دسکتاپ پوشه ShafieeSolutions)
        string targetDirectory = context.Command.GetOption("output") ?? context.Command.GetOption("path") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionDirectory = Path.Combine(targetDirectory, solutionName);

        try
        {
            // ۱. ساخت پوشه اصلی Solution
            if (!Directory.Exists(solutionDirectory))
            {
                Directory.CreateDirectory(solutionDirectory);
            }

            // ۲. ساخت فایل کانفیگ اختصاصی shafiee.json
            SaveSolutionConfig(solutionDirectory, solutionName, archName);

            // ۳. ساخت فایل .sln اصلی با دستور dotnet
            RunDotnetCommand($"new sln -n {solutionName}", solutionDirectory);

            // ۴. ساخت پروژه‌ها و زیرساخت پوشه‌ها بر اساس معماری انتخابی
            GenerateDetailedArchitectureProjects(solutionDirectory, archName, solutionName);

            context.IsSuccess = true;
            context.Diagnostics.Add(new Diagnostic($"Solution '{solutionName}' scaffolded successfully at: {solutionDirectory}", DiagnosticSeverity.Info));
        }
        catch (Exception ex)
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Failed to scaffold solution: {ex.Message}", DiagnosticSeverity.Error));
        }

        return Task.CompletedTask;
    }

    private static void SaveSolutionConfig(string rootDir, string solutionName, string architecture)
    {
        string configPath = Path.Combine(rootDir, "shafiee.json");
        string jsonContent = $@"{{
  ""SolutionName"": ""{solutionName}"",
  ""Architecture"": ""{architecture}"",
  ""CreatedAt"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}""
}}";
        File.WriteAllText(configPath, jsonContent);
    }

    private static void GenerateDetailedArchitectureProjects(string rootDir, string architecture, string solutionName)
    {
        var layerDefinitions = architecture.ToLower() switch
        {
            "clean" => new Dictionary<string, (string Type, string[] Folders)>
            {
                { "Domain", ("classlib", new[] { "Entities", "ValueObjects", "Exceptions" }) },
                { "Application", ("classlib", new[] { "Interfaces", "Services", "DTOs" }) },
                { "Infrastructure", ("classlib", new[] { "Persistence", "Repositories" }) },
                { "Presentation", ("webapi", new[] { "Controllers" }) },
                { "Tests", ("xunit", new[] { "UnitTests" }) }
            },
            "microservice" => new Dictionary<string, (string Type, string[] Folders)>
            {
                { "IdentityService", ("webapi", new[] { "Controllers", "Models" }) },
                { "OrderService", ("webapi", new[] { "Controllers", "Services" }) },
                { "ApiGateway", ("webapi", new[] { "Routes" }) },
                { "Common", ("classlib", new[] { "Exceptions", "Middleware" }) }
            },
            _ => new Dictionary<string, (string Type, string[] Folders)> // Layered پیش‌فرض
            {
                { "Presentation", ("webapi", new[] { "Controllers" }) },
                { "Domain", ("classlib", new[] { "Entities" }) },
                { "Data", ("classlib", new[] { "Context", "Configurations" }) },
                { "Validation", ("classlib", new[] { "Products" }) },
                { "Business", ("classlib", new[] { "DTOs\\Products" }) },
                { "Tests", ("xunit", new[] { "UnitTests" }) }
            }
        };

        foreach (var layer in layerDefinitions)
        {
            string layerName = layer.Key;
            string projectType = layer.Value.Type;
            string[] subFolders = layer.Value.Folders;

            string projectName = $"{solutionName}.{layerName}";

            string layerBasePath = layerName == "Tests"
                ? Path.Combine(rootDir, "tests")
                : Path.Combine(rootDir, "src");

            Directory.CreateDirectory(layerBasePath);

            // ساخت پروژه با دستور دات‌نت
            RunDotnetCommand($"new {projectType} -n {projectName}", layerBasePath);

            string projectFolder = Path.Combine(layerBasePath, projectName);

            foreach (var subFolder in subFolders)
            {
                string fullSubFolderPath = Path.Combine(projectFolder, subFolder);
                Directory.CreateDirectory(fullSubFolderPath);
            }

            string csprojPath = Path.Combine(projectFolder, $"{projectName}.csproj");
            string solutionFilePath = Path.Combine(rootDir, $"{solutionName}.sln");

            if (File.Exists(csprojPath))
            {
                RunDotnetCommand($"sln \"{solutionFilePath}\" add \"{csprojPath}\"", rootDir);
            }
        }
    }

    private static void RunDotnetCommand(string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            process.WaitForExit();
        }
    }
}