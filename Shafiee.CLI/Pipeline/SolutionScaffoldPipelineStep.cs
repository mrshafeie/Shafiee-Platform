namespace Shafiee.CLI.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Shafiee.Architectures;
using Shafiee.Architectures.Enterprise;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Pipeline;
using Shafiee.SDK.Results;

public class SolutionScaffoldPipelineStep : IPipelineStep
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
        string archName = context.Command.GetOption("arch") ?? context.Command.GetOption("architecture") ?? "enterprise";

        // اعتبارسنجی معماری با استفاده از ArchitectureRegistry
        var arch = ArchitectureRegistry.Get(archName);
        if (arch == null)
        {
            var supported = string.Join(", ", ArchitectureRegistry.GetAll().Select(a => a.Name));
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Invalid architecture: '{archName}'. Supported: {supported}", DiagnosticSeverity.Error));
            return Task.CompletedTask;
        }

        // تعیین مسیر خروجی (پیش‌فرض: دسکتاپ پوشه ShafieeSolutions)
        string targetDirectory = context.Command.GetOption("output") ?? context.Command.GetOption("path") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions");
        string solutionDirectory = Path.Combine(targetDirectory, solutionName);

        try
        {
            // ۱. ساخت پوشه اصلی Solution
            if (!Directory.Exists(solutionDirectory))
            {
                Directory.CreateDirectory(solutionDirectory);
            }

            // ۲. ساخت فایل کانفیگ اختصاصی shafiee.json با ساختار تفکیک‌شده Core و Business
            SaveSolutionConfig(solutionDirectory, solutionName, arch.Name, DefaultCoreModules, DefaultBusinessModules);

            // ۳. ساخت فایل .sln اصلی با دستور dotnet
            RunDotnetCommand($"new sln -n {solutionName}", solutionDirectory);

            // ۴. ساخت پروژه‌ها و زیرساخت پوشه‌ها بر اساس معماری انتخابی
            if (arch.Name.Equals("enterprise", StringComparison.OrdinalIgnoreCase))
            {
                var enterpriseArch = arch as EnterpriseArchitecture ?? new EnterpriseArchitecture();
                GenerateEnterpriseArchitectureProjects(solutionDirectory, solutionName, enterpriseArch, DefaultCoreModules, DefaultBusinessModules);
            }
            else
            {
                GenerateDetailedArchitectureProjects(solutionDirectory, arch.Name, solutionName);
            }

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

    private static void SaveSolutionConfig(string rootDir, string solutionName, string architecture, IEnumerable<string> coreModules, IEnumerable<string> businessModules)
    {
        string configPath = Path.Combine(rootDir, "shafiee.json");
        var configObj = new
        {
            project = new
            {
                name = solutionName,
                architecture = architecture,
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            },
            modules = new
            {
                core = coreModules,
                business = businessModules
            }
        };

        string jsonContent = JsonSerializer.Serialize(configObj, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, jsonContent);
    }

    private static void GenerateEnterpriseArchitectureProjects(string rootDir, string solutionName, EnterpriseArchitecture arch, IEnumerable<string> coreModules, IEnumerable<string> businessModules)
    {
        // الف) ایجاد ساختار پوشه‌ها و فایل‌های .gitkeep از روی متد BuildStructureWithModules
        foreach (var file in arch.BuildStructureWithModules(solutionName, coreModules, businessModules))
        {
            string fullPath = Path.Combine(rootDir, file.Path);
            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (file.Path.EndsWith(".gitkeep", StringComparison.OrdinalIgnoreCase) && !File.Exists(fullPath))
            {
                File.WriteAllText(fullPath, string.Empty);
            }
        }

        string solutionFilePath = Path.Combine(rootDir, $"{solutionName}.sln");

        // ب) ۱. ساخت پروژه‌های BuildingBlocks
        var buildingBlocks = new[] { "Shared", "Domain", "Application", "Validation", "Persistence", "Messaging", "Security", "Caching", "Observability" };
        foreach (var bb in buildingBlocks)
        {
            string projName = $"{solutionName}.BuildingBlocks.{bb}";
            string targetDir = Path.Combine(rootDir, "src", "BuildingBlocks", projName);
            Directory.CreateDirectory(targetDir);

            RunDotnetCommand($"new classlib -n {projName}", targetDir);
            RegisterProjectToSolution(solutionFilePath, targetDir, projName, rootDir);
        }

        // ب) ۲. ساخت پروژه‌های Core Modules
        foreach (var module in coreModules)
        {
            CreateModuleProjects(rootDir, solutionName, "Core", module, solutionFilePath);
        }

        // ب) ۳. ساخت پروژه‌های Business Modules
        foreach (var module in businessModules)
        {
            CreateModuleProjects(rootDir, solutionName, "Business", module, solutionFilePath);
        }

        // ب) ۴. ساخت پروژه Contracts
        {
            string contractsProj = $"{solutionName}.Contracts";
            string contractsDir = Path.Combine(rootDir, "src", "Contracts", contractsProj);
            Directory.CreateDirectory(contractsDir);

            RunDotnetCommand($"new classlib -n {contractsProj}", contractsDir);
            RegisterProjectToSolution(solutionFilePath, contractsDir, contractsProj, rootDir);
        }

        // ب) ۵. ساخت پروژه‌های Hosts
        var hosts = new Dictionary<string, string>
        {
            { "Api", "webapi" },
            { "Mvc", "mvc" },
            { "RazorPages", "web" },
            { "Blazor", "blazor" },
            { "Grpc", "grpc" },
            { "SignalR", "webapi" },
            { "WebSocket", "webapi" },
            { "GraphQL", "webapi" },
            { "OData", "webapi" },
            { "Worker", "worker" },
            { "WebHooks", "webapi" },
            { "Cli", "console" },
            { "Desktop.WinForms", "winforms" },
            { "Mobile.Maui", "maui" },
            { "Mcp", "webapi" }
        };

        foreach (var host in hosts)
        {
            string projName = $"{solutionName}.{host.Key}";
            string targetDir = Path.Combine(rootDir, "src", "Hosts", projName);
            Directory.CreateDirectory(targetDir);

            RunDotnetCommand($"new {host.Value} -n {projName}", targetDir);
            RegisterProjectToSolution(solutionFilePath, targetDir, projName, rootDir);
        }

        // ب) ۶. ساخت پروژه‌های Tests
        var tests = new[] { "Unit", "Integration", "Architecture", "Contract", "Functional", "Performance", "EndToEnd", "Load" };
        foreach (var test in tests)
        {
            string projName = $"{solutionName}.{test}Tests";
            string targetDir = Path.Combine(rootDir, "tests", test, projName);
            Directory.CreateDirectory(targetDir);

            RunDotnetCommand($"new xunit -n {projName}", targetDir);
            RegisterProjectToSolution(solutionFilePath, targetDir, projName, rootDir);
        }
    }

    private static void CreateModuleProjects(string rootDir, string solutionName, string category, string moduleName, string solutionFilePath)
    {
        var moduleLayers = new Dictionary<string, string>
        {
            { "Domain", "classlib" },
            { "Application", "classlib" },
            { "Infrastructure", "classlib" },
            { "Api", "webapi" }
        };

        foreach (var layer in moduleLayers)
        {
            string projName = $"{solutionName}.{moduleName}.{layer.Key}";
            string targetDir = Path.Combine(rootDir, "src", "Modules", category, moduleName, projName);
            Directory.CreateDirectory(targetDir);

            RunDotnetCommand($"new {layer.Value} -n {projName}", targetDir);
            RegisterProjectToSolution(solutionFilePath, targetDir, projName, rootDir);
        }
    }

    private static void RegisterProjectToSolution(string solutionFilePath, string projectDirectory, string projectName, string rootDir)
    {
        string csprojPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        if (File.Exists(csprojPath))
        {
            RunDotnetCommand($"sln \"{solutionFilePath}\" add \"{csprojPath}\"", rootDir);
        }
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
            _ => new Dictionary<string, (string Type, string[] Folders)>
            {
                { "Presentation", ("webapi", new[] { "Controllers" }) },
                { "Domain", ("classlib", new[] { "Entities" }) }
            }
        };

        foreach (var layer in layerDefinitions)
        {
            string projectName = $"{solutionName}.{layer.Key}";
            string layerBasePath = layer.Key == "Tests" ? Path.Combine(rootDir, "tests") : Path.Combine(rootDir, "src");
            Directory.CreateDirectory(layerBasePath);

            RunDotnetCommand($"new {layer.Value.Type} -n {projectName}", layerBasePath);
            string projectFolder = Path.Combine(layerBasePath, projectName);

            foreach (var subFolder in layer.Value.Folders)
            {
                Directory.CreateDirectory(Path.Combine(projectFolder, subFolder));
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