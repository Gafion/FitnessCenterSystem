using Assembly = System.Reflection.Assembly;
using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace FitnessCenter.ArchitectureTests;

/// <summary>
/// Finds every module under <c>src/Modules/</c> and loads its two assemblies
/// (D-031): <c>&lt;Module&gt;</c> (internals) and <c>&lt;Module&gt;.Contracts</c> (public).
/// A new module folder is picked up automatically; the tests fail until this
/// project references both of its assemblies.
/// </summary>
internal static class Modules
{
    public static IReadOnlyList<string> Names { get; } = FindModuleNames();

    public static IReadOnlyList<Assembly> Internals { get; } =
        Names.Select(name => Assembly.Load(name)).ToList();

    public static IReadOnlyList<Assembly> Contracts { get; } =
        Names.Select(name => Assembly.Load($"{name}.Contracts")).ToList();

    public static Architecture Architecture { get; } =
        new ArchLoader().LoadAssemblies([.. Internals, .. Contracts]).Build();

    public static string RootNamespace(string module) => $"FitnessCenter.Modules.{module}";

    private static List<string> FindModuleNames()
    {
        var modulesDir = Path.Combine(FindRepoRoot(), "src", "Modules");
        return Directory.GetDirectories(modulesDir)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FitnessCenter.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not find FitnessCenter.sln above the test output folder.");
    }
}
