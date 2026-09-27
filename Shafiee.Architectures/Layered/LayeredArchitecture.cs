namespace Shafiee.Architectures.Layered;

using System.Collections.Generic;
using System.IO;
using Shafiee.SDK.Architectures;

public class LayeredArchitecture : IArchitecture
{
    public string Name => "layered";
    public string DisplayName => "N-Tier Layered Architecture";
    public string Description => "Classic N-Tier architecture (Entities, Repositories, Services, Controllers) for standard applications.";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return new List<ArchitectureFile>
        {
            new() { Path = $"src/{solutionName}.Entities/.gitkeep" },
            new() { Path = $"src/{solutionName}.DataAccess/.gitkeep" },
            new() { Path = $"src/{solutionName}.Business/.gitkeep" },
            new() { Path = $"src/{solutionName}.WebApi/.gitkeep" }
        };
    }

    // بازگرداندن مسیر نسبی برای هماهنگی کامل با DiskArtifactWriter
    public string ResolveArtifactPath(string solutionName, string entityName, string artifactType)
    {
        return artifactType switch
        {
            "Dto" => Path.Combine("src", $"{solutionName}.Business", "DTOs", $"{entityName}s", $"{entityName}Dto.cs"),
            "Validator" => Path.Combine("src", $"{solutionName}.Business", "Validators", $"{entityName}s", $"{entityName}Validator.cs"),
            "Repository" => Path.Combine("src", $"{solutionName}.DataAccess", "Repositories", $"{entityName}Repository.cs"),
            "Service" => Path.Combine("src", $"{solutionName}.Business", "Services", $"{entityName}Service.cs"),
            "Configuration" => Path.Combine("src", $"{solutionName}.DataAccess", "Configurations", $"{entityName}Configuration.cs"),
            "Context" => Path.Combine("src", $"{solutionName}.DataAccess", "Context", $"{solutionName}DbContext.cs"),
            "Entity" => Path.Combine("src", $"{solutionName}.Entities", "Entities", $"{entityName}.cs"),
            _ => Path.Combine("src", $"{solutionName}.WebApi", "Controllers", $"{entityName}Controller.cs")
        };
    }
}