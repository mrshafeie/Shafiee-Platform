namespace Shafiee.Generators.Generators;

using System;
using System.Collections.Generic;
using Scriban;
using Scriban.Runtime;
using Shafiee.Generators.Contracts;
using Shafiee.Generators.Infrastructure;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class RepositoryGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        string templateContent = TemplateLoader.LoadTemplate(architecture, "RepositoryTemplate.scriban");
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            string errors = string.Join(" | ", template.Messages);
            throw new Exception($"Error parsing RepositoryTemplate.scriban: {errors}");
        }

        string entityName = metadata.Name;
        string ns = $"{solutionName}.DataAccess.Repositories";
        string entityNs = $"{solutionName}.Domain.Entities";
        string contextNs = $"{solutionName}.DataAccess.Context";

        var scriptObject = new ScriptObject();
        scriptObject.Add("namespace", ns);
        scriptObject.Add("entity_namespace", entityNs);
        scriptObject.Add("context_namespace", contextNs);
        scriptObject.Add("entity_name", entityName);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        string generatedCode = template.Render(context);

        string relativePath = architecture.ResolveArtifactPath(solutionName, entityName, "Repository");
        string fileName = System.IO.Path.GetFileName(relativePath);

        yield return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = relativePath,
            Type = ArtifactType.SourceCode
        };
    }
}