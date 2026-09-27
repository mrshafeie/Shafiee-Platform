namespace Shafiee.Generators.Generators;

using System;
using System.IO;
using Scriban;
using Shafiee.SDK.Artifacts;
using Shafiee.SDK.Architectures;

public class DependencyInjectionGenerator
{
    public Artifact Generate(string solutionName, IArchitecture architecture)
    {
        // ۱. مسیر فایل قالب DependencyInjection
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "DependencyInjectionTemplate.scriban");

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"فایل قالب DependencyInjection در مسیر {templatePath} یافت نشد!");
        }

        string templateContent = File.ReadAllText(templatePath);
        var template = Template.Parse(templateContent);

        if (template.HasErrors)
        {
            throw new Exception("خطا در پارس کردن قالب DependencyInjectionTemplate.scriban");
        }

        // ۲. آماده‌سازی مدل داده
        var model = new
        {
            Namespace = $"{solutionName}.Infrastructure"
        };

        // ۳. رندر کردن کد نهایی
        string generatedCode = template.Render(model);

        // ۴. ساخت مسیر دستی ذخیره فایل
        var fileName = "DependencyInjection.cs";
        // مسیر: MySolution/src/MySolution.Infrastructure/DependencyInjection.cs
        var relativePath = Path.Combine(solutionName, "src", $"{solutionName}.Infrastructure", fileName);

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