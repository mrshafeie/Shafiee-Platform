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

public class CommandHandlerGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        // ۱. مسیر فایل قالب CommandHandler
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "CommandHandlerTemplate.scriban");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"فایل قالب CommandHandler در مسیر {templatePath} یافت نشد!");
        }

        string templateContent = File.ReadAllText(templatePath);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب CommandHandlerTemplate.scriban");
        }

        // ۲. تنظیم فضا‌نام پویا متناسب با معماری لایه‌ای (لایه Business)
        string targetNamespace = $"{solutionName}.Business.Features.{metadata.Name}s.Commands";

        var scriptObject = new ScriptObject
        {
            { "Namespace", targetNamespace },
            { "Name", metadata.Name },
            { "Properties", metadata.Properties }
        };

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        // ۳. رندر کردن کد با کانتکست جدید
        string generatedCode = template.Render(context);

        // ۴. استفاده از متد پویای معماری برای گرفتن مسیر دقیق ذخیره فایل
        string fileName = $"Create{metadata.Name}CommandHandler.cs";
        string filePath = architecture.ResolveArtifactPath(solutionName, metadata.Name, "CommandHandler");

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