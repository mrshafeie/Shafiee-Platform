namespace Shafiee.SDK.Metadata;

public class EntityMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<PropertyMetadata> Properties { get; set; } = new();
    public List<RelationMetadata> Relations { get; set; } = new();
}
