namespace Shafiee.SDK.Context;

using Shafiee.SDK.Metadata;

public interface IPackageContext
{
    string SolutionName { get; }
    string RootPath { get; }

    // تمام متاداده‌های استخراج شده از پروژه
    SolutionMetadata Metadata { get; }

    // روش ساده برای ثبت فایل‌های خروجی تولید شده
    void AddArtifact(string relativePath, string content);
}