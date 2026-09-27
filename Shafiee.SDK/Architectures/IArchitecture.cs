namespace Shafiee.SDK.Architectures;

public interface IArchitecture
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }

    /// <summary>
    /// لیست پوشه‌ها و فایل‌های پایه که باید ساخته شوند
    /// </summary>
    IEnumerable<ArchitectureFile> BuildStructure(string solutionName);
    string ResolveArtifactPath(string solutionName, string entityName, string artifactType);
}

public class ArchitectureFile
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}