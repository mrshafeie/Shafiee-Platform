namespace Shafiee.Architectures.Clean;

using Shafiee.SDK.Architectures;

public class CleanArchitecture : IArchitecture
{
    public string Name => "clean";
    public string DisplayName => "Clean Architecture";
    public string Description => "Classic 4-layer Clean Architecture (Domain, Application, Infrastructure, Presentation).";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return new List<ArchitectureFile>
        {
            new() { Path = $"src/Domain/{solutionName}.Domain/.gitkeep" },
            new() { Path = $"src/Application/{solutionName}.Application/.gitkeep" },
            new() { Path = $"src/Infrastructure/{solutionName}.Infrastructure/.gitkeep" },
            new() { Path = $"src/Presentation/{solutionName}.Api/.gitkeep" }
        };
    }

    public string ResolveArtifactPath(string targetType, string moduleOrFeatureName, string fileName)
    {
        return targetType.ToLower() switch
        {
            "entity" => $"src/Domain/{{SolutionName}}.Domain/Entities/{moduleOrFeatureName}/{fileName}",
            "dto" => $"src/Application/{{SolutionName}}.Application/{moduleOrFeatureName}/Generated/DTOs/{fileName}",
            "command" or "query" => $"src/Application/{{SolutionName}}.Application/{moduleOrFeatureName}/Generated/Features/{fileName}",
            "validator" => $"src/Application/{{SolutionName}}.Application/{moduleOrFeatureName}/Generated/Validators/{fileName}",
            "dbcontext" or "configuration" => $"src/Infrastructure/{{SolutionName}}.Infrastructure/Generated/Persistence/{fileName}",
            "endpoint" or "controller" => $"src/Presentation/{{SolutionName}}.Api/Controllers/{moduleOrFeatureName}/{fileName}",
            "unittest" => $"tests/{{SolutionName}}.UnitTests/{fileName}",
            _ => $"src/Application/{{SolutionName}}.Application/{moduleOrFeatureName}/Custom/{fileName}"
        };
    }
}