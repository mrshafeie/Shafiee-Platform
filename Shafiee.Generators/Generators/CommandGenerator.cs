namespace Shafiee.Generators.Generators;

using System;
using System.IO;
using System.Collections.Generic;
using Scriban;
using Scriban.Runtime; // برای ScriptObject
using Shafiee.Generators.Contracts;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class CommandGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "CreateCommandTemplate.scriban");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"فایل قالب Command در مسیر {templatePath} یافت نشد!");
        }

        string templateContent = File.ReadAllText(templatePath);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب CreateCommandTemplate.scriban");
        }

        // تنظیم فضا‌نام متناسب با معماری لایه‌ای (لایه Business)
        string targetNamespace = $"{solutionName}.Business.Features.{metadata.Name}s.Commands";

        var scriptObject = new ScriptObject
        {
            { "Namespace", targetNamespace },
            { "Name", metadata.Name },
            { "Properties", metadata.Properties }
        };

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        string generatedCode = template.Render(context);

        // استفاده از متد پویای معماری برای گرفتن مسیر دقیق
        string fileName = $"Create{metadata.Name}Command.cs";
        string filePath = architecture.ResolveArtifactPath(solutionName, metadata.Name, "Command");

        yield return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = filePath,
            Type = ArtifactType.SourceCode
        };
    }
}