namespace Shafiee.SDK.Commands;

public class UserCommand
{
    public required string Verb { get; init; }      // new, add, generate, analyze, doctor, ...
    public required string Resource { get; init; }  // solution, project, module, crud, api, ui, ...
    public string? Target { get; init; }            // Product, Shop.Web, ...
    public IDictionary<string, string> Options { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string Name => $"{Verb} {Resource}";

    public string? GetOption(string key)
    {
        return Options.TryGetValue(key, out var value) ? value : null;
    }
}