namespace Shafiee.Generators.Generators;

using System;
using System.Collections.Generic;
using Scriban;
using Shafiee.Generators.Infrastructure;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class DbContextGenerator
{
    public Artifact Generate(IEnumerable<EntityMetadata> entities, string solutionName, IArchitecture architecture)
    {
        // ۱. بارگذاری پویا و معماری‌پذیر قالب DbContext با استفاده از TemplateLoader
        string templateContent = TemplateLoader.LoadTemplate(architecture, "DbContextTemplate.scriban");

        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب DbContextTemplate.scriban");
        }

        // ۲. آماده‌سازی مدل داده
        var model = new
        {
            Namespace = $"{solutionName}.DataAccess.Context",
            Entities = entities
        };

        // ۳. رندر کردن کد نهایی
        string generatedCode = template.Render(model);

        // ۴. دریافت مسیر پویا و استاندارد بر اساس نوع معماری انتخابی کاربر
        // از آنجایی که DbContext سراسری است، از کلید "Context" استفاده می‌کنیم
        string relativePath = architecture.ResolveArtifactPath(solutionName, string.Empty, "Context");
        string fileName = System.IO.Path.GetFileName(relativePath);

        // ۵. خروجی نهایی
        return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = relativePath,
            Type = ArtifactType.SourceCode
        };
    }
}