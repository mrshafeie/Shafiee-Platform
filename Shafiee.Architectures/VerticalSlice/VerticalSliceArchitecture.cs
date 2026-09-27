namespace Shafiee.Architectures.VerticalSlice;

using Shafiee.SDK.Architectures;

public class VerticalSliceArchitecture : IArchitecture
{
    public string Name => "vertical";
    public string DisplayName => "Vertical Slice Architecture";
    public string Description => "Feature-centric architecture combining Domain, Application, and Presentation inside individual slices.";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return new List<ArchitectureFile>
        {
            new() { Path = $"src/{solutionName}.Core/.gitkeep" },
            new() { Path = $"src/{solutionName}.Features/.gitkeep" }
        };
    }

    public string ResolveArtifactPath(string targetType, string moduleOrFeatureName, string fileName)
    {
        string featureBasePath = $"src/{{SolutionName}}.Features/Features/{moduleOrFeatureName}";

        return targetType.ToLower() switch
        {
            "entity" => $"src/{{SolutionName}}.Core/Entities/{fileName}",
            "command" or "query" or "dto" or "validator" or "endpoint" => $"{featureBasePath}/Generated/{fileName}",
            "dbcontext" or "configuration" => $"src/{{SolutionName}}.Features/Persistence/{fileName}",
            "unittest" => $"tests/{{SolutionName}}.Tests/Features/{moduleOrFeatureName}/{fileName}",
            _ => $"{featureBasePath}/Custom/{fileName}"
        };
    }
}