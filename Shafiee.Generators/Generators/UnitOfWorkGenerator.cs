namespace Shafiee.Generators.Generators;

using System;
using System.IO;
using Scriban;
using Shafiee.Generators.Infrastructure;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class UnitOfWorkGenerator
{
    public Artifact Generate(string solutionName, IArchitecture architecture)
    {
        // ۱. بارگذاری پویا و معماری‌پذیر قالب UnitOfWork با استفاده از TemplateLoader
        string templateContent = TemplateLoader.LoadTemplate(architecture, "UnitOfWorkTemplate.scriban");

        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب UnitOfWorkTemplate.scriban");
        }

        // ۲. آماده‌سازی مدل داده
        var model = new
        {
            Namespace = $"{solutionName}.DataAccess.UnitOfWork",
            SolutionName = solutionName
        };

        // ۳. رندر کردن کد نهایی
        string generatedCode = template.Render(model);

        // ۴. دریافت مسیر پویا بر اساس معماری انتخابی
        string relativePath = architecture.ResolveArtifactPath(solutionName, string.Empty, "UnitOfWork");
        string fileName = Path.GetFileName(relativePath);

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