namespace Shafiee.Analysis.Parsers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shafiee.SDK.Metadata;

public class CSharpEntityParser
{
    public EntityMetadata ParseEntityCode(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetCompilationUnitRoot();

        var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null)
            throw new InvalidOperationException("هیچ کلاسی در کد متنی یافت نشد.");

        var entity = new EntityMetadata
        {
            Name = classDeclaration.Identifier.Text,
            Namespace = classDeclaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ?? string.Empty
        };

        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();

        foreach (var prop in properties)
        {
            var propName = prop.Identifier.Text;
            var propType = prop.Type.ToString();

            entity.Properties.Add(new PropertyMetadata
            {
                Name = propName,
                Type = propType,
                IsPrimaryKey = propName.Equals("Id", StringComparison.OrdinalIgnoreCase),
                IsNullable = propType.EndsWith("?")
            });
        }

        return entity;
    }
}