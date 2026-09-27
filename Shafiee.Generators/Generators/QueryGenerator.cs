namespace Shafiee.Generators.Generators;

using System;
using System.IO;
using System.Collections.Generic;
using Scriban;
using Scriban.Runtime;
using Shafiee.Generators.Contracts;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class QueryGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        // ۱. مسیر فایل قالب Query
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "GetByIdQueryTemplate.scriban");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"فایل قالب Query در مسیر {templatePath} یافت نشد!");
        }

        string templateContent = File.ReadAllText(templatePath);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب GetByIdQueryTemplate.scriban");
        }

        // ۲. تنظیم فضا‌نام پویا متناسب با معماری لایه‌ای (لایه Business)
        string targetNamespace = $"{solutionName}.Business.Features.{metadata.Name}s.Queries";

        var scriptObject = new ScriptObject
        {
            { "Namespace", targetNamespace },
            { "Name", metadata.Name }
        };

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        // ۳. رندر کردن کد با کانتکست جدید
        string generatedCode = template.Render(context);

        // ۴. استفاده از متد پویای معماری برای گرفتن مسیر دقیق ذخیره فایل
        string fileName = $"Get{metadata.Name}ByIdQuery.cs";
        string filePath = architecture.ResolveArtifactPath(solutionName, metadata.Name, "Query");

        // ۵. خروجی
        yield return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = filePath,
            Type = ArtifactType.SourceCode
        };
    }
}