namespace Shafiee.Generators.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Scriban;
using Scriban.Runtime;
using Shafiee.Generators.Contracts;
using Shafiee.Generators.Infrastructure;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class ValidatorGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        // 1. Load the template using TemplateLoader
        string templateContent = TemplateLoader.LoadTemplate(architecture, "ValidatorTemplate.scriban");

        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            string scribanErrors = string.Join(" | ", template.Messages);
            throw new Exception($"Error parsing ValidatorTemplate.scriban: {scribanErrors}");
        }

        string entityName = metadata.Name;
        string ns = $"{solutionName}.Business.Validators";
        string dtoNs = ns.Replace(".Validators", ".DTOs");

        // 2. Use ScriptObject to pass data cleanly and reliably to Scriban
        var scriptObject = new ScriptObject();
        scriptObject.Add("namespace", ns);
        scriptObject.Add("dto_namespace", dtoNs);
        scriptObject.Add("entity_name", entityName);
        scriptObject.Add("properties", metadata.Properties);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        // 3. Render final code
        string generatedCode = template.Render(context);

        // 4. Resolve dynamic path based on architecture
        string relativePath = architecture.ResolveArtifactPath(solutionName, entityName, "Validator");
        string fileName = System.IO.Path.GetFileName(relativePath);

        // 5. Yield final artifact
        yield return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = relativePath,
            Type = ArtifactType.SourceCode
        };
    }
}