namespace Shafiee.SDK.Metadata;

public class SolutionMetadata
{
    public string Name { get; set; } = string.Empty;
    public List<ProjectMetadata> Projects { get; set; } = new();
    public List<EntityMetadata> Entities { get; set; } = new();
    public List<EnumMetadata> Enums { get; set; } = new();
}
