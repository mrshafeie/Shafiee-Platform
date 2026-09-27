namespace Shafiee.Generators.Generators;

using System;
using System.Collections.Generic;
using Scriban;
using Shafiee.Generators.Contracts;
using Shafiee.Generators.Infrastructure;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class ControllerGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        // ۱. بارگذاری پویا و معماری‌پذیر قالب Controller با استفاده از TemplateLoader
        string templateContent = TemplateLoader.LoadTemplate(architecture, "ControllerTemplate.scriban");

        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب ControllerTemplate.scriban");
        }

        // ۲. آماده‌سازی مدل داده
        var model = new
        {
            Namespace = $"{solutionName}.WebApi.Controllers",
            Name = metadata.Name
        };

        // ۳. رندر کردن کد
        string generatedCode = template.Render(model);

        // ۴. دریافت مسیر پویا و استاندارد بر اساس نوع معماری انتخابی کاربر
        string relativePath = architecture.ResolveArtifactPath(solutionName, metadata.Name, "Controller");
        string fileName = System.IO.Path.GetFileName(relativePath);

        // ۵. خروجی
        yield return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = relativePath,
            Type = ArtifactType.SourceCode
        };
    }
}