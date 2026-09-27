namespace Shafiee.CLI.Writers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shafiee.SDK.Abstractions;
using Shafiee.SDK.Artifacts;

public class DiskArtifactWriter : IArtifactWriter
{
    private string _baseDirectory = Directory.GetCurrentDirectory();

    public void SetBaseDirectory(string directory)
    {
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _baseDirectory = directory;
        }
    }

    public async Task WriteAsync(IEnumerable<Artifact> artifacts, CancellationToken cancellationToken = default)
    {
        if (artifacts == null)
        {
            Console.WriteLine("⚠️ [Debug] Artifacts collection is null!");
            return;
        }

        int successCount = 0;
        int skipCount = 0;

        foreach (var artifact in artifacts)
        {
            if (string.IsNullOrEmpty(artifact?.RelativePath) || artifact.Content == null)
            {
                Console.WriteLine($"⚠️ [Debug] Skipped an artifact due to empty path or content.");
                continue;
            }

            var fullPath = Path.Combine(_baseDirectory, artifact.RelativePath);
            Console.WriteLine($"🔍 [Debug] Target path: {fullPath}");

            if (File.Exists(fullPath))
            {
                Console.WriteLine($"⚠️ [Debug] File already exists (skipped): {fullPath}");
                skipCount++;
                continue;
            }

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                // اصلاح نام پارامتر به cancellationToken
                await File.WriteAllTextAsync(fullPath, artifact.Content, cancellationToken);
                Console.WriteLine($"✅ [Written] {fullPath}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [Error writing file] {fullPath}: {ex.Message}");
            }
        }

        Console.WriteLine($"\n📊 [Disk Summary] Successfully wrote {successCount} files, Skipped {skipCount} existing files.");
    }
}