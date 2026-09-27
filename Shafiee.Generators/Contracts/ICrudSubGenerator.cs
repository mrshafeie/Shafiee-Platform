namespace Shafiee.Generators.Contracts;

using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;
using Shafiee.SDK.Metadata;

public interface ICrudSubGenerator
{
    IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture);
}