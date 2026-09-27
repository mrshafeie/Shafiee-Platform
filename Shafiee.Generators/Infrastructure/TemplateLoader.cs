namespace Shafiee.Generators.Infrastructure;

using System;
using System.IO;
using Shafiee.SDK.Architectures;

public static class TemplateLoader
{
    public static string LoadTemplate(IArchitecture architecture, string templateFileName)
    {
        // ۱. استخراج نام معماری (مثلا LayeredArchitecture تبدیل می‌شود به Layered)
        string archName = architecture.GetType().Name.Replace("Architecture", "");

        // ۲. بررسی مسیر اختصاصی معماری (مثلا Templates/Layered/DtoTemplate.scriban)
        string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", archName, templateFileName);

        // ۳. Fallback: اگر قالب اختصاصی نبود، از پوشه پیش‌فرض استفاده کن
        if (!File.Exists(templatePath))
        {
            templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Default", templateFileName);
        }

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"فایل قالب در مسیر {templatePath} یافت نشد!");
        }

        return File.ReadAllText(templatePath);
    }
    }
