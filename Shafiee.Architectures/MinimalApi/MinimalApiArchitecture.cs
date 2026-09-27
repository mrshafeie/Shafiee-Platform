namespace Shafiee.Architectures.MinimalApi;

using Shafiee.SDK.Architectures;

public class MinimalApiArchitecture : IArchitecture
{
    public string Name => "minimalapi";
    public string DisplayName => "Minimal API Architecture";
    public string Description => "Ultra-lightweight & high-performance architecture optimized for Microservices and fast APIs.";

    public IEnumerable<ArchitectureFile> BuildStructure(string solutionName)
    {
        return new List<ArchitectureFile>
        {
            new() { Path = $"src/{solutionName}.Api/.gitkeep" }
        };
    }

    public string ResolveArtifactPath(string targetType, string moduleOrFeatureName, string fileName)
    {
        string apiBasePath = "src/{SolutionName}.Api";

        return targetType.ToLower() switch
        {
            "entity" => $"{apiBasePath}/Entities/{fileName}",
            "dto" => $"{apiBasePath}/DTOs/{fileName}",
            "endpoint" => $"{apiBasePath}/Endpoints/{moduleOrFeatureName}/{fileName}",
            "dbcontext" or "configuration" => $"{apiBasePath}/Data/{fileName}",
            "unittest" => $"tests/{{SolutionName}}.Tests/{fileName}",
            _ => $"{apiBasePath}/Services/{fileName}"
        };
    }
}