using Shafiee.Generators.Generators;
using Shafiee.SDK.Architectures;
using Shafiee.SDK.Artifacts;
using Shafiee.SDK.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shafiee.Generators.Engines
{
    public class ExecutionEngine
    {
        private readonly IArchitecture _architecture;

        public ExecutionEngine(IArchitecture architecture)
        {
            _architecture = architecture;
        }

        public void GenerateCode(string solutionName, List<EntityMetadata> entities)
        {
            var allArtifacts = new List<Artifact>();

            // ۱. مقداردهی اولیه ژنراتورها
            var dtoGen = new DtoGenerator();
            var commandGen = new CommandGenerator();
            var queryGen = new QueryGenerator();
            var controllerGen = new ControllerGenerator();
            var handlerGen = new CommandHandlerGenerator();
            var configGen = new ConfigurationGenerator();
            var validatorGen = new ValidatorGenerator();
            var dbContextGen = new DbContextGenerator();
            var diGen = new DependencyInjectionGenerator();

            // ۲. حلقه روی تک‌تک انتیتی‌ها و تولید فایل‌های CRUD
            foreach (var entity in entities)
            {
                allArtifacts.AddRange(dtoGen.Generate(entity, solutionName, _architecture));
                allArtifacts.AddRange(commandGen.Generate(entity, solutionName, _architecture));
                allArtifacts.AddRange(queryGen.Generate(entity, solutionName, _architecture));
                allArtifacts.AddRange(controllerGen.Generate(entity, solutionName, _architecture));
                allArtifacts.AddRange(handlerGen.Generate(entity, solutionName, _architecture));
                allArtifacts.AddRange(configGen.Generate(entity, solutionName, _architecture));
                allArtifacts.AddRange(validatorGen.Generate(entity, solutionName, _architecture));
            }

            // ۳. تولید فایل‌های سراسری (Global Artifacts)
            allArtifacts.Add(dbContextGen.Generate(entities, solutionName, _architecture));
            allArtifacts.Add(diGen.Generate(solutionName, _architecture));

            // ۴. نوشتن تمام آرتیفکت‌ها روی هارد دیسک
            foreach (var artifact in allArtifacts)
            {
                WriteArtifactToDisk(artifact);
            }

            Console.WriteLine($"[Success] تمام فایل‌ها برای {entities.Count} انتیتی با موفقیت تولید و ذخیره شدند!");
        }

        private void WriteArtifactToDisk(Artifact artifact)
        {
            // اطمینان از اینکه پوشه مقصد وجود دارد
            string? directoryPath = Path.GetDirectoryName(artifact.RelativePath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // نوشتن محتوای متنی کد روی فایل
            File.WriteAllText(artifact.RelativePath, artifact.Content);
            Console.WriteLine($"Created: {artifact.RelativePath}");
        }
    }
}
