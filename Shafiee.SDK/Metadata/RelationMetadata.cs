namespace Shafiee.SDK.Metadata;

public class RelationMetadata
{
    public string PropertyName { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string RelationType { get; set; } = string.Empty; // OneToMany, ManyToOne, ...
}
