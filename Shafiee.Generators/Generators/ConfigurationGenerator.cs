namespace Shafiee.Generators.Generators;

using System;
using System.Collections.Generic;
using System.Linq;
using Scriban;
using Shafiee.Generators.Contracts;
using Shafiee.Generators.Infrastructure;
using Shafiee.SDK.Metadata;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;

public class ConfigurationGenerator : ICrudSubGenerator
{
    public IEnumerable<Artifact> Generate(EntityMetadata metadata, string solutionName, IArchitecture architecture)
    {
        // ۱. بارگذاری پویا و معماری‌پذیر قالب Configuration با استفاده از TemplateLoader
        string templateContent = TemplateLoader.LoadTemplate(architecture, "ConfigurationTemplate.scriban");

        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب ConfigurationTemplate.scriban");
        }

        // ۲. استخراج کلید اصلی برای تنظیم در مدل
        var keyProp = metadata.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var entityName = metadata.Name;
        var moduleName = string.IsNullOrEmpty(metadata.Namespace) ? entityName : metadata.Namespace;

        // ۳. آماده‌سازی مدل داده
        var model = new
        {
            Namespace = $"{solutionName}.DataAccess.Configurations",
            EntityName = entityName,
            TableName = entityName.ToLower() + "s",
            KeyPropName = keyProp?.Name,
            Properties = metadata.Properties
        };

        // ۴. رندر کردن کد نهایی
        string generatedCode = template.Render(model);

        // ۵. دریافت مسیر پویا و استاندارد بر اساس نوع معماری انتخابی کاربر
        string relativePath = architecture.ResolveArtifactPath(solutionName, entityName, "Configuration");
        string fileName = System.IO.Path.GetFileName(relativePath);

        // ۶. خروجی نهایی
        yield return new Artifact
        {
            Name = fileName,
            Content = generatedCode,
            RelativePath = relativePath,
            Type = ArtifactType.SourceCode
        };
    }
}