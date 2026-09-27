namespace Shafiee.SDK.Generators;

using Shafiee.SDK.Metadata;

public interface IGenerator
{
    string Name { get; }

    /// <summary>
    /// تولید خروجی بر اساس متاداده ورودی
    /// </summary>
    string Generate(EntityMetadata entity, SolutionMetadata solution);
}