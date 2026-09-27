namespace Shafiee.Architectures.Ddd;

using Shafiee.SDK.Architectures;

public class DddArchitecture : IArchitecture
{
    public string Name => "ddd";
    public string DisplayName => "Domain-Driven Design (DDD)";
    public string Description => "Enterprise DDD architecture structured around Bounded Contexts, Aggregates, and SharedKernel.";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return new List<ArchitectureFile>
        {
            new() { Path = $"src/SharedKernel/{solutionName}.SharedKernel/.gitkeep" },
            new() { Path = "src/Contexts/.gitkeep" },
            new() { Path = $"src/Hosts/{solutionName}.Api/.gitkeep" }
        };
    }

    public string ResolveArtifactPath(string targetType, string moduleOrFeatureName, string fileName)
    {
        string contextBasePath = $"src/Contexts/{moduleOrFeatureName}";

        return targetType.ToLower() switch
        {
            "entity" or "aggregate" or "valueobject" or "domainevent" => $"{contextBasePath}/Domain/Model/{fileName}",
            "dto" => $"{contextBasePath}/Application/Generated/DTOs/{fileName}",
            "command" or "query" => $"{contextBasePath}/Application/Generated/CommandsQueries/{fileName}",
            "validator" => $"{contextBasePath}/Application/Generated/Validators/{fileName}",
            "dbcontext" or "configuration" or "repository" => $"{contextBasePath}/Infrastructure/Generated/Persistence/{fileName}",
            "endpoint" or "controller" => $"{contextBasePath}/Presentation/Generated/Controllers/{fileName}",
            "unittest" => $"{contextBasePath}/Tests/Unit/{fileName}",
            _ => $"{contextBasePath}/Custom/{fileName}"
        };
    }
}