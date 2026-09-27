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

public class DtoGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        string templateContent = TemplateLoader.LoadTemplate(architecture, "DtoTemplate.scriban");
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب DtoTemplate.scriban");
        }

        // استفاده از ScriptObject برای تزریق دقیق و بدون خطای داده‌ها به Scriban
        var scriptObject = new ScriptObject();
        scriptObject.Add("namespace", $"{solutionName}.Business.DTOs");
        scriptObject.Add("name", metadata.Name);
        scriptObject.Add("properties", metadata.Properties);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        // رندر کردن قالب با Context اختصاصی
        string generatedCode = template.Render(context);

        string relativePath = architecture.ResolveArtifactPath(solutionName, metadata.Name, "Dto");
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