namespace Shafiee.CLI.Pipeline;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shafiee.SDK.Pipeline;
using Shafiee.SDK.Results;

public class SolutionReferencePipelineStep : IPipelineStep
{
    private static readonly string[] DefaultCoreModules = {
        "Identity", "Users", "Roles", "Permissions", "Organizations",
        "Notifications", "Files", "Audit", "Settings", "Localization"
    };

    private static readonly string[] DefaultBusinessModules = {
        "Catalog", "Customers", "Orders", "Inventory", "Payments"
    };

    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        string solutionName = context.Command.Target ?? "MySolution";
        string targetDirectory = context.Command.GetOption("output") ?? context.Command.GetOption("path") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionDirectory = Path.Combine(targetDirectory, solutionName);

        if (!Directory.Exists(solutionDirectory))
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Solution directory not found: {solutionDirectory}", DiagnosticSeverity.Error));
            return Task.CompletedTask;
        }

        try
        {
            // ۱. لینک کردن لایه‌های داخلی هر ماژول (Core و Business)
            foreach (var module in DefaultCoreModules)
            {
                WireModuleReferences(solutionDirectory, solutionName, "Core", module);
            }

            foreach (var module in DefaultBusinessModules)
            {
                WireModuleReferences(solutionDirectory, solutionName, "Business", module);
            }

            // ۲. لینک کردن BuildingBlocks به لایه‌ها در صورت نیاز
            // (مثلاً تزریق Shared BuildingBlock به Domain یا Application)
            WireBuildingBlockReferences(solutionDirectory, solutionName);

            context.IsSuccess = true;
            context.Diagnostics.Add(new Diagnostic("All project references wired successfully.", DiagnosticSeverity.Info));
        }
        catch (Exception ex)
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Failed to wire references: {ex.Message}", DiagnosticSeverity.Error));
        }

        return Task.CompletedTask;
    }

    private static void WireModuleReferences(string rootDir, string solutionName, string category, string moduleName)
    {
        string moduleBasePath = Path.Combine(rootDir, "src", "Modules", category, moduleName);

        string domainProj = Path.Combine(moduleBasePath, $"{solutionName}.{moduleName}.Domain", $"{solutionName}.{moduleName}.Domain.csproj");
        string appProj = Path.Combine(moduleBasePath, $"{solutionName}.{moduleName}.Application", $"{solutionName}.{moduleName}.Application.csproj");
        string infraProj = Path.Combine(moduleBasePath, $"{solutionName}.{moduleName}.Infrastructure", $"{solutionName}.{moduleName}.Infrastructure.csproj");
        string apiProj = Path.Combine(moduleBasePath, $"{solutionName}.{moduleName}.Api", $"{solutionName}.{moduleName}.Api.csproj");

        // جریان وابستگی تمیز (Clean Architecture داخل هر ماژول):
        // Application -> Domain
        if (File.Exists(appProj) && File.Exists(domainProj))
        {
            RunDotnetAddReference(appProj, domainProj, rootDir);
        }

        // Infrastructure -> Application & Domain
        if (File.Exists(infraProj))
        {
            if (File.Exists(appProj)) RunDotnetAddReference(infraProj, appProj, rootDir);
            if (File.Exists(domainProj)) RunDotnetAddReference(infraProj, domainProj, rootDir);
        }

        // Api -> Application & Infrastructure
        if (File.Exists(apiProj))
        {
            if (File.Exists(appProj)) RunDotnetAddReference(apiProj, appProj, rootDir);
            if (File.Exists(infraProj)) RunDotnetAddReference(apiProj, infraProj, rootDir);
        }
    }

    private static void WireBuildingBlockReferences(string rootDir, string solutionName)
    {
        // در صورت تمایل می‌توانیم BuildingBlocks عمومی را به لایه‌های Domain یا Application لینک کنیم
        // مثال: Domain بیلدینگ بلاک به پروژه‌های Domain ماژول‌ها
        string sharedBbProj = Path.Combine(rootDir, "src", "BuildingBlocks", $"{solutionName}.BuildingBlocks.Shared", $"{solutionName}.BuildingBlocks.Shared.csproj");

        if (File.Exists(sharedBbProj))
        {
            foreach (var module in DefaultCoreModules.Concat(DefaultBusinessModules))
            {
                string domainProj = Path.Combine(rootDir, "src", "Modules", DefaultCoreModules.Contains(module) ? "Core" : "Business", module, $"{solutionName}.{module}.Domain", $"{solutionName}.{module}.Domain.csproj");
                if (File.Exists(domainProj))
                {
                    RunDotnetAddReference(domainProj, sharedBbProj, rootDir);
                }
            }
        }
    }

    private static void RunDotnetAddReference(string targetProject, string referencedProject, string workingDir)
    {
        if (!File.Exists(targetProject) || !File.Exists(referencedProject)) return;

        string arguments = $"add \"{targetProject}\" reference \"{referencedProject}\"";

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();
    }
}