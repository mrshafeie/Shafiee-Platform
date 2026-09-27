namespace Shafiee.Architectures.Enterprise;

using Shafiee.SDK.Architectures;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

public class EnterpriseArchitecture : IArchitecture
{
    public string Name => "enterprise";
    public string DisplayName => "Enterprise Multi-Layered Architecture";
    public string Description => "معماری انترپرایز لایه‌ای کامل با تفکیک دقیق Common، Domain، Infrastructure، Application، Services، Presentation و Shared";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        var files = new List<ArchitectureFile>
        {
            // --- 1. Common Project ---
            new() { Path = $"src/Common/{solutionName}.Common/Abstractions/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Base/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Constants/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Exceptions/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Extensions/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Helpers/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Localization/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Options/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Results/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Security/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Utilities/.gitkeep" },
            new() { Path = $"src/Common/{solutionName}.Common/Validation/.gitkeep" },

            // --- 2. Domain Project ---
            new() { Path = $"src/Domain/{solutionName}.Domain/Base/.gitkeep" },
            new() { Path = $"src/Domain/{solutionName}.Domain/Entities/.gitkeep" },
            new() { Path = $"src/Domain/{solutionName}.Domain/ValueObjects/.gitkeep" },
            new() { Path = $"src/Domain/{solutionName}.Domain/Enums/.gitkeep" },
            new() { Path = $"src/Domain/{solutionName}.Domain/Events/.gitkeep" },
            new() { Path = $"src/Domain/{solutionName}.Domain/Specifications/.gitkeep" },
            new() { Path = $"src/Domain/{solutionName}.Domain/Configurations/.gitkeep" },

            // --- 3. Infrastructure Project ---
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Persistence/Contexts/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Persistence/Configurations/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Persistence/Migrations/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Persistence/Repositories/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Persistence/Seed/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Persistence/DbExtensions/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Dapper/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/ExternalServices/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Logging/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Caching/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Storage/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/Identity/.gitkeep" },

            // --- 4. Application Project ---
            new() { Path = $"src/Application/{solutionName}.Application/Features/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/DTOs/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/ViewModels/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/Reports/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/Procedures/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/Validators/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/Mapping/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/Contracts/.gitkeep" },

            // --- 5. Services Project ---
            new() { Path = $"src/Services/{solutionName}.Services/Business/.gitkeep" },
            new() { Path = $"src/Services/{solutionName}.Services/Reporting/.gitkeep" },
            new() { Path = $"src/Services/{solutionName}.Services/Integration/.gitkeep" },
            new() { Path = $"src/Services/{solutionName}.Services/BackgroundJobs/.gitkeep" },
            new() { Path = $"src/Services/{solutionName}.Services/Notification/.gitkeep" },
            new() { Path = $"src/Services/{solutionName}.Services/InMemory/.gitkeep" },

            // --- 6. Presentation Projects ---
            new() { Path = $"src/Presentation/{solutionName}.Api/.gitkeep" },
            new() { Path = $"src/Presentation/{solutionName}.Web/.gitkeep" },
            new() { Path = $"src/Presentation/{solutionName}.Admin/.gitkeep" },

            // --- 7. Shared Project ---
            new() { Path = $"src/Shared/{solutionName}.Shared/Resources/.gitkeep" },
            new() { Path = $"src/Shared/{solutionName}.Shared/Contracts/.gitkeep" },
            new() { Path = $"src/Shared/{solutionName}.Shared/Models/.gitkeep" },

            // --- 8. Tests, Docs, Build ---
            new() { Path = $"tests/{solutionName}.UnitTests/.gitkeep" },
            new() { Path = $"tests/{solutionName}.IntegrationTests/.gitkeep" },
            new() { Path = $"tests/{solutionName}.PerformanceTests/.gitkeep" },
            new() { Path = "docs/.gitkeep" },
            new() { Path = "build/.gitkeep" }
        };

        return files;
    }

    // هماهنگ‌سازی امضای متد با IArchitecture (solutionName, entityName, artifactType)
    public string ResolveArtifactPath(string solutionName, string entityName, string artifactType)
    {
        return artifactType.ToLower() switch
        {
            "entity" => Path.Combine("src", "Domain", $"{solutionName}.Domain", "Entities", $"{entityName}.cs"),
            "valueobject" => Path.Combine("src", "Domain", $"{solutionName}.Domain", "ValueObjects", $"{entityName}.cs"),
            "enum" => Path.Combine("src", "Domain", $"{solutionName}.Domain", "Enums", $"{entityName}.cs"),

            "dto" => Path.Combine("src", "Application", $"{solutionName}.Application", "DTOs", $"{entityName}s", $"{entityName}Dto.cs"),
            "viewmodel" => Path.Combine("src", "Application", $"{solutionName}.Application", "ViewModels", $"{entityName}s", $"{entityName}ViewModel.cs"),
            "command" => Path.Combine("src", "Application", $"{solutionName}.Application", "Features", $"{entityName}s", "Commands", $"Create{entityName}Command.cs"),
            "commandhandler" => Path.Combine("src", "Application", $"{solutionName}.Application", "Features", $"{entityName}s", "Commands", $"Create{entityName}CommandHandler.cs"),
            "query" => Path.Combine("src", "Application", $"{solutionName}.Application", "Features", $"{entityName}s", "Queries", $"Get{entityName}ByIdQuery.cs"),
            "validator" => Path.Combine("src", "Application", $"{solutionName}.Application", "Validators", $"{entityName}s", $"{entityName}Validator.cs"),
            "mapping" => Path.Combine("src", "Application", $"{solutionName}.Application", "Mapping", $"{entityName}MappingExtensions.cs"),

            "configuration" => Path.Combine("src", "Infrastructure", $"{solutionName}.Infrastructure", "Persistence", "Configurations", $"{entityName}Configuration.cs"),
            "dbcontext" => Path.Combine("src", "Infrastructure", $"{solutionName}.Infrastructure", "Persistence", "Contexts", $"{solutionName}DbContext.cs"),
            "repository" => Path.Combine("src", "Infrastructure", $"{solutionName}.Infrastructure", "Persistence", "Repositories", $"{entityName}Repository.cs"),

            "service" => Path.Combine("src", "Services", $"{solutionName}.Services", "Business", $"{entityName}Service.cs"),

            "controller" or "endpoint" => Path.Combine("src", "Presentation", $"{solutionName}.Api", "Controllers", $"{entityName}Controller.cs"),

            "unittest" => Path.Combine("tests", $"{solutionName}.UnitTests", $"{entityName}Tests.cs"),
            "integrationtest" => Path.Combine("tests", $"{solutionName}.IntegrationTests", $"{entityName}IntegrationTests.cs"),

            _ => Path.Combine("src", "Application", $"{solutionName}.Application", "Features", $"{entityName}s", $"{entityName}.cs")
        };
    }
}