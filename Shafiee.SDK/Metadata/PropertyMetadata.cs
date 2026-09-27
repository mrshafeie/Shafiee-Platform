namespace Shafiee.SDK.Metadata;

public class PropertyMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
}
