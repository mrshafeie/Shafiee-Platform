namespace Shafiee.CLI.Pipeline;

using Shafiee.Analysis;
using Shafiee.Architectures;
using Shafiee.Generators.Contracts;
using Shafiee.Generators.Generators;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Pipeline;
using Shafiee.SDK.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class CrudGeneratorPipelineStep : IPipelineStep
{
    public Task ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        string solutionName = context.Command.GetOption("solution") ?? context.Command.Target ?? "MySolution";

        // تعیین هوشمندانه مسیر ریشه سولشن (اولویت با دسکتاپ یا مسیری که از قبل وجود دارد)
        string desktopSolutionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ShafieeSolutions", solutionName);
        string solutionRoot;

        if (Directory.Exists(desktopSolutionPath))
        {
            solutionRoot = desktopSolutionPath;
        }
        else
        {
            string currentDir = Directory.GetCurrentDirectory();
            if (Directory.Exists(Path.Combine(currentDir, "src", $"{solutionName}.Entities")) ||
                Directory.Exists(Path.Combine(currentDir, "src", $"{solutionName}.Domain")) ||
                Directory.Exists(Path.Combine(currentDir, "src", "Domain")))
            {
                solutionRoot = currentDir;
            }
            else
            {
                solutionRoot = desktopSolutionPath;
            }
        }

        // تنظیم مسیر Entityها بر اساس ساختارهای مختلف (Enterprise, Layered, Clean)
        // ۱. ساختار Enterprise
        string entitiesDir = Path.Combine(solutionRoot, "src", "Domain", $"{solutionName}.Domain", "Entities");

        if (!Directory.Exists(entitiesDir))
        {
            // ۲. ساختار Layered
            entitiesDir = Path.Combine(solutionRoot, "src", $"{solutionName}.Entities", "Entities");
        }

        if (!Directory.Exists(entitiesDir))
        {
            entitiesDir = Path.Combine(solutionRoot, "src", $"{solutionName}.Entities");
        }

        if (!Directory.Exists(entitiesDir))
        {
            // ۳. ساختار Clean Architecture
            entitiesDir = Path.Combine(solutionRoot, "src", $"{solutionName}.Domain", "Entities");
        }

        if (!Directory.Exists(entitiesDir))
        {
            entitiesDir = Path.Combine(solutionRoot, "src", $"{solutionName}.Domain");
        }

        if (!Directory.Exists(entitiesDir) && Directory.Exists(Path.Combine(solutionRoot, "Entities")))
        {
            entitiesDir = Path.Combine(solutionRoot, "Entities");
        }

        if (!Directory.Exists(entitiesDir))
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Entities directory for solution '{solutionName}' not found at '{entitiesDir}'.", DiagnosticSeverity.Error));
            return Task.CompletedTask;
        }

        var entityFiles = Directory.GetFiles(entitiesDir, "*.cs", SearchOption.AllDirectories);
        if (entityFiles.Length == 0)
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"No entity files found in directory '{entitiesDir}'.", DiagnosticSeverity.Warning));
            return Task.CompletedTask;
        }

        // پیش‌فرض را روی انترپرایز بگذاریم
        string archName = "enterprise";
        string configPath = Path.Combine(solutionRoot, "shafiee.json");

        if (File.Exists(configPath))
        {
            var configContent = File.ReadAllText(configPath);
            if (configContent.Contains("clean")) archName = "clean";
            else if (configContent.Contains("microservice")) archName = "microservice";
            else if (configContent.Contains("layered")) archName = "layered";
            else if (configContent.Contains("enterprise")) archName = "enterprise";
        }
        else
        {
            // اگر فایل کانفیگ نبود، به طور خودکار آن را با پیش‌فرض انترپرایز ایجاد کن
            try
            {
                string defaultConfig = "{\n  \"architecture\": \"enterprise\"\n}";
                File.WriteAllText(configPath, defaultConfig);
            }
            catch
            {
                // در صورت بروز خطا در نوشتن فایل، خللی در روند اجرا ایجاد نکند
            }
        }

        var arch = ArchitectureRegistry.Get(archName);
        if (arch == null)
        {
            context.IsSuccess = false;
            context.Diagnostics.Add(new Diagnostic($"Architecture '{archName}' not found.", DiagnosticSeverity.Error));
            return Task.CompletedTask;
        }

        

        var allMetadata = new List<EntityMetadata>();
        var analyzer = new CodeAnalyzer();

        var crudGenerators = new List<ICrudSubGenerator>
        {
            new DtoGenerator(),
            new CommandGenerator(),
            new CommandHandlerGenerator(),
            new QueryGenerator(),
            new ControllerGenerator(),
            new ConfigurationGenerator(),
            new ValidatorGenerator(),
            new RepositoryGenerator(),
            new ServiceGenerator()
        };

        foreach (var entityFilePath in entityFiles)
        {
            var sourceCode = File.ReadAllText(entityFilePath);
            var metadata = analyzer.AnalyzeEntity(sourceCode);

            if (metadata != null)
            {
                allMetadata.Add(metadata);

                foreach (var gen in crudGenerators)
                {
                    var artifacts = gen.Generate(metadata, solutionName, arch);
                    if (artifacts != null)
                    {
                        context.Artifacts.AddRange(artifacts);
                    }
                }
            }
        }

        if (allMetadata.Count > 0)
        {
            var dbContextGen = new DbContextGenerator();
            var dbArtifacts = dbContextGen.Generate(allMetadata, solutionName, arch);
            if (dbArtifacts != null)
            {
                context.Artifacts.AddRange(dbArtifacts);
            }
        }

        var diGen = new DependencyInjectionGenerator();
        var diArtifacts = diGen.Generate(solutionName, arch);
        if (diArtifacts != null)
        {
            context.Artifacts.AddRange(diArtifacts);
        }

        context.IsSuccess = true;
        context.Diagnostics.Add(new Diagnostic($"Successfully scanned {allMetadata.Count} entities and generated CRUD artifacts.", DiagnosticSeverity.Info));
        return Task.CompletedTask;
    }
}