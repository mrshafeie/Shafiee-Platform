namespace Shafiee.CLI.Pipeline;

using System;
using System.Collections.Generic;
using Shafiee.SDK.Commands;
using Shafiee.SDK.Pipeline;
using Shafiee.CLI.Writers;

public static class PackageRouter
{
    public static IEnumerable<IPipelineStep> ResolveSteps(UserCommand command)
    {
        var steps = new List<IPipelineStep>();

        // ۱. دستور ساخت راهکار جدید: shafiee new solution Shop --arch enterprise
        if (command.Verb.Equals("new", StringComparison.OrdinalIgnoreCase) &&
            command.Resource.Equals("solution", StringComparison.OrdinalIgnoreCase))
        {
            // ابتدا ساختار پوشه‌ها و پروژه‌ها ایجاد می‌شود
            steps.Add(new SolutionScaffoldPipelineStep());

            // بلافاصله ارجاعات و Referenceهای بین پروژه‌ها به صورت اتوماتیک متصل می‌شوند
            steps.Add(new SolutionReferencePipelineStep());
        }
        // ۲. دستور تولید کد CRUD: shafiee generate crud Product
        else if (command.Verb.Equals("generate", StringComparison.OrdinalIgnoreCase) &&
                 command.Resource.Equals("crud", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new CrudGeneratorPipelineStep());

            // اضافه کردن مرحله نوشتن فایل‌ها روی دیسک به پایپ‌لاین
            steps.Add(new DiskWriterStep(new DiskArtifactWriter()));
        }
        // ۳. دستور تولید ساختار پروژه Shared: shafiee generate shared --solution Shop
        else if (command.Verb.Equals("generate", StringComparison.OrdinalIgnoreCase) &&
                 command.Resource.Equals("shared", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new SharedGeneratorPipelineStep());
        }

        return steps;
    }
}