namespace Shafiee.Architectures;

using Shafiee.SDK.Architectures;
using Shafiee.Architectures.Enterprise;
using Shafiee.Architectures.Clean;
using Shafiee.Architectures.Ddd;
using Shafiee.Architectures.Modular;
using Shafiee.Architectures.VerticalSlice;
using Shafiee.Architectures.Layered;
using Shafiee.Architectures.MinimalApi;

public static class ArchitectureRegistry
{
    private static readonly Dictionary<string, IArchitecture> _architectures = new(StringComparer.OrdinalIgnoreCase)
    {
        { "enterprise", new EnterpriseArchitecture() },
        { "clean", new CleanArchitecture() },
        { "ddd", new DddArchitecture() },
        { "modular", new ModularArchitecture() },
        { "vertical", new VerticalSliceArchitecture() },
        { "layered", new LayeredArchitecture() },
        { "minimalapi", new MinimalApiArchitecture() }
    };

    public static IArchitecture? Get(string name)
    {
        _architectures.TryGetValue(name, out var architecture);
        return architecture;
    }

    public static IEnumerable<IArchitecture> GetAll() => _architectures.Values;
}