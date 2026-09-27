namespace Shafiee.Analysis;

using Shafiee.Analysis.Models;
using Shafiee.Analysis.Parsers;
using Shafiee.SDK.Metadata;

public class CodeAnalyzer
{
    private readonly CSharpEntityParser _entityParser;

    public CodeAnalyzer()
    {
        _entityParser = new CSharpEntityParser();
    }

    public EntityMetadata AnalyzeEntity(string entitySourceCode)
    {
        return _entityParser.ParseEntityCode(entitySourceCode);
    }
}