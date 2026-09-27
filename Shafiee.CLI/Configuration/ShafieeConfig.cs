namespace Shafiee.CLI.Configuration;

using System.Text.Json;

public class ShafieeConfigModel
{
    public string RootPath { get; set; } = string.Empty;
    public string SolutionName { get; set; } = string.Empty;
}

public static class ShafieeConfig
{
    private const string ConfigFileName = ".shafieeconfig";

    /// <summary>
    /// ذخیره مسیر و نام سلوشن به عنوان Global / Active Context
    /// </summary>
    public static void SaveGlobalContext(string rootPath, string solutionName)
    {
        var config = new ShafieeConfigModel
        {
            RootPath = rootPath,
            SolutionName = solutionName
        };

        var filePath = Path.Combine(rootPath, ConfigFileName);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// جستجوی هوشمند کانفیگ از پوشه جاری به سمت پوشه‌های بالاتر
    /// </summary>
    public static ShafieeConfigModel? FindAndLoad(string currentDirectory)
    {
        var dir = new DirectoryInfo(currentDirectory);

        while (dir != null)
        {
            var configPath = Path.Combine(dir.FullName, ConfigFileName);
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<ShafieeConfigModel>(json);
                }
                catch
                {
                    return null;
                }
            }
            dir = dir.Parent;
        }

        return null;
    }
}