namespace Shafiee.SDK.Abstractions;

public interface IProjectExplorer
{
    Task<string?> FindEntityFilePathAsync(string entityName, string searchDirectory);
    Task<string> GetProjectRootDirectoryAsync(string startDirectory);
}