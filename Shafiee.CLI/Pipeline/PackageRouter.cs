namespace Shafiee.CLI.Pipeline;

using Shafiee.SDK.Commands;
using Shafiee.SDK.Pipeline;
using Shafiee.CLI.Writers; // اضافه کردن فضای نام نویسنده

public static class PackageRouter
{
    public static IEnumerable<IPipelineStep> ResolveSteps(UserCommand command)
    {
        var steps = new List<IPipelineStep>();

        // ۱. دستور ساخت راهکار جدید: shafiee new solution Shop --arch enterprise
        if (command.Verb.Equals("new", StringComparison.OrdinalIgnoreCase) &&
            command.Resource.Equals("solution", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new SolutionScaffoldPipelineStep());
        }
        // ۲. دستور تولید کد CRUD: shafiee generate crud Product
        else if (command.Verb.Equals("generate", StringComparison.OrdinalIgnoreCase) &&
                 command.Resource.Equals("crud", StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new CrudGeneratorPipelineStep());

            // اضافه کردن مرحله نوشتن فایل‌ها روی دیسک به پایپ‌لاین
            steps.Add(new DiskWriterStep(new DiskArtifactWriter()));
        }

        return steps;
    }
}