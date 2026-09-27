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

public class ServiceGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        string templateContent = TemplateLoader.LoadTemplate(architecture, "ServiceTemplate.scriban");
        var template = Template.Parse(templateContent);

        // اصلاح شده: فقط بررسی template.HasErrors کافی است
        if (template.HasErrors)
        {
            string errors = string.Join(" | ", template.Messages);
            throw new Exception($"Error parsing ServiceTemplate.scriban: {errors}");
        }

        string entityName = metadata.Name;
        string ns = $"{solutionName}.Business.Services";

        var scriptObject = new ScriptObject();
        scriptObject.Add("namespace", ns);
        scriptObject.Add("contracts_namespace", $"{solutionName}.Business.Contracts");
        scriptObject.Add("dtos_namespace", $"{solutionName}.Business.DTOs");
        scriptObject.Add("repositories_namespace", $"{solutionName}.DataAccess.Repositories");
        scriptObject.Add("unitofwork_namespace", $"{solutionName}.DataAccess.UnitOfWork");
        scriptObject.Add("entity_namespace", $"{solutionName}.Domain.Entities");
        scriptObject.Add("entity_name", entityName);
        scriptObject.Add("properties", metadata.Properties);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        string generatedCode = template.Render(context);

        string relativePath = architecture.ResolveArtifactPath(solutionName, entityName, "Service");
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