namespace Shafiee.Architectures.Modular;

using Shafiee.SDK.Architectures;

public class ModularArchitecture : IArchitecture
{
    public string Name => "modular";
    public string DisplayName => "Modular Monolith Architecture";
    public string Description => "Enterprise-grade Modular Monolith architecture with Framework, Modules, Packages, and Hosts isolation.";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return new List<ArchitectureFile>
        {
            new() { Path = $"src/Framework/{solutionName}.Domain/.gitkeep" },
            new() { Path = $"src/Framework/{solutionName}.Application/.gitkeep" },
            new() { Path = $"src/Framework/{solutionName}.Shared/.gitkeep" },
            new() { Path = $"src/Framework/{solutionName}.Infrastructure/.gitkeep" },
            new() { Path = "src/Modules/.gitkeep" },
            new() { Path = "src/Packages/.gitkeep" },
            new() { Path = $"src/Hosts/{solutionName}.Api/.gitkeep" }
        };
    }

    public string ResolveArtifactPath(string targetType, string moduleOrFeatureName, string fileName)
    {
        string baseModulePath = $"src/Modules/{moduleOrFeatureName}";

        return targetType.ToLower() switch
        {
            "entity" => $"{baseModulePath}/Domain/Entities/{fileName}",
            "dto" => $"{baseModulePath}/Application/Generated/DTOs/{fileName}",
            "command" or "query" => $"{baseModulePath}/Application/Generated/Features/{fileName}",
            "validator" => $"{baseModulePath}/Application/Generated/Validators/{fileName}",
            "dbcontext" or "configuration" => $"{baseModulePath}/Infrastructure/Generated/Persistence/{fileName}",
            "endpoint" or "controller" => $"{baseModulePath}/Presentation/Generated/Endpoints/{fileName}",
            "manifest" => $"{baseModulePath}/module.json",
            "unittest" => $"{baseModulePath}/Tests/Unit/{fileName}",
            _ => $"{baseModulePath}/Custom/{fileName}"
        };
    }
}