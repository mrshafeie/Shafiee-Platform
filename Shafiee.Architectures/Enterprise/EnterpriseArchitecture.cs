namespace Shafiee.Architectures.Enterprise;

using Shafiee.SDK.Architectures;
using System.Collections.Generic;
using System.IO;

public class EnterpriseArchitecture : IArchitecture
{
    public string Name => "enterprise";
    public string DisplayName => "Enterprise Modular Monolith & Master Architecture";
    public string Description => "معماری پیشرفته انترپرایز ماژولار با تفکیک دقیق ماژول‌های Core و Business، BuildingBlocks و هاست‌های جامع";

    // لیست کامل و استاندارد ماژول‌های Core و Business مطابق درخت معماری ماتریکس
    private static readonly string[] DefaultCoreModules = {
        "Identity", "Users", "Roles", "Permissions", "Organizations",
        "Notifications", "Files", "Audit", "Settings", "Localization"
    };

    private static readonly string[] DefaultBusinessModules = {
        "Catalog", "Customers", "Orders", "Inventory", "Payments"
    };

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return BuildStructureWithModules(solutionName, DefaultCoreModules, DefaultBusinessModules);
    }

    public IEnumerable<ArchitectureFile> BuildStructureWithModules(string solutionName, IEnumerable<string> coreModules, IEnumerable<string> businessModules)
    {
        var files = new List<ArchitectureFile>();

        // --- 1. BuildingBlocks ---
        var buildingBlocks = new[]
        {
            "Shared", "Domain", "Application", "Validation",
            "Persistence", "Messaging", "Security", "Caching", "Observability"
        };

        foreach (var bb in buildingBlocks)
        {
            files.Add(new ArchitectureFile { Path = $"src/BuildingBlocks/{solutionName}.BuildingBlocks.{bb}/.gitkeep" });
        }

        // --- 2. Modules: Core ---
        foreach (var module in coreModules)
        {
            AddModuleLayerFiles(files, solutionName, "Core", module);
        }

        // --- 3. Modules: Business ---
        foreach (var module in businessModules)
        {
            AddModuleLayerFiles(files, solutionName, "Business", module);
        }

        // --- 4. Shared Infrastructure ---
        var infrastructures = new[]
        {
            "Database/SqlServer", "Database/PostgreSql", "Database/MySql", "Database/Sqlite",
            "Messaging/RabbitMQ", "Messaging/Kafka", "Messaging/AzureServiceBus",
            "Caching/Memory", "Caching/Redis",
            "Storage/Local", "Storage/S3", "Storage/AzureBlob", "Storage/MinIO",
            "Search/Elasticsearch", "Search/OpenSearch",
            "Communication/Email", "Communication/SMS", "Communication/Push",
            "Payment", "AI", "ExternalServices"
        };

        foreach (var infra in infrastructures)
        {
            files.Add(new ArchitectureFile { Path = $"src/Infrastructure/{infra}/.gitkeep" });
        }

        // --- 5. Hosts ---
        var hosts = new[]
        {
            "Api", "Mvc", "RazorPages", "Blazor", "Grpc",
            "SignalR", "WebSocket", "GraphQL", "OData",
            "Worker", "WebHooks", "Cli", "Desktop.WinForms", "Mobile.Maui", "Mcp"
        };

        foreach (var host in hosts)
        {
            files.Add(new ArchitectureFile { Path = $"src/Hosts/{solutionName}.{host}/.gitkeep" });
        }

        // --- 6. Contracts ---
        files.Add(new ArchitectureFile { Path = $"src/Contracts/{solutionName}.Contracts/.gitkeep" });

        // --- 7. Tests ---
        var testTypes = new[]
        {
            "Unit", "Integration", "Architecture", "Contract",
            "Functional", "Performance", "Load", "EndToEnd"
        };

        foreach (var test in testTypes)
        {
            files.Add(new ArchitectureFile { Path = $"tests/{test}/{solutionName}.{test}Tests/.gitkeep" });
        }

        // --- 8. Docs & Build ---
        files.Add(new ArchitectureFile { Path = "docs/.gitkeep" });
        files.Add(new ArchitectureFile { Path = "build/.gitkeep" });

        return files;
    }

    private static void AddModuleLayerFiles(List<ArchitectureFile> files, string solutionName, string category, string module)
    {
        string basePath = $"src/Modules/{category}/{module}";

        // Domain Project Folders
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Domain/Entities/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Domain/ValueObjects/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Domain/Events/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Domain/Specifications/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Domain/Exceptions/.gitkeep" });

        // Application Project Folders
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Application/Commands/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Application/Queries/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Application/DTOs/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Application/Contracts/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Application/Validators/.gitkeep" });

        // Infrastructure Project Folders
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Infrastructure/Persistence/DbContext/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Infrastructure/Persistence/Configurations/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Infrastructure/Repositories/.gitkeep" });

        // Api Project Folders
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Api/Controllers/.gitkeep" });
        files.Add(new ArchitectureFile { Path = $"{basePath}/{solutionName}.{module}.Api/Endpoints/.gitkeep" });
    }

    public string ResolveArtifactPath(string solutionName, string entityName, string artifactType)
    {
        string targetModule = "Catalog";
        string category = "Business";

        return artifactType.ToLower() switch
        {
            "entity" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Domain", "Entities", $"{entityName}.cs"),
            "valueobject" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Domain", "ValueObjects", $"{entityName}.cs"),
            "dto" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Application", "DTOs", $"{entityName}Dto.cs"),
            "command" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Application", "Commands", $"Create{entityName}Command.cs"),
            "query" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Application", "Queries", $"Get{entityName}ByIdQuery.cs"),
            "repository" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Infrastructure", "Repositories", $"{entityName}Repository.cs"),
            "controller" => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Api", "Controllers", $"{entityName}Controller.cs"),
            "unittest" => Path.Combine("tests", "Unit", $"{solutionName}.UnitTests", $"{entityName}Tests.cs"),
            _ => Path.Combine("src", "Modules", category, targetModule, $"{solutionName}.{targetModule}.Application", $"{entityName}.cs")
        };
    }
}