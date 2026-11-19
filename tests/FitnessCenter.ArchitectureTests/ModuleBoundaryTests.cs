using System.Text.RegularExpressions;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace FitnessCenter.ArchitectureTests;

/// <summary>
/// Module boundary rules from CLAUDE.md and architecture.md §4.2, enforced in CI (D-028).
/// Do not weaken, skip or delete these tests to make a build pass.
/// </summary>
[TestClass]
public sealed class ModuleBoundaryTests
{
    [TestMethod]
    public void At_least_two_modules_are_found()
    {
        Assert.IsGreaterThanOrEqualTo(2, Modules.Names.Count, string.Join(", ", Modules.Names));
    }

    /// <summary>
    /// Rule 1: a module may use another module's Contracts only, never its internals.
    /// Rule 3 (no reading another module's tables) follows from this: each module's
    /// DbContext lives in its internal assembly, so other modules cannot reach it.
    /// </summary>
    [TestMethod]
    public void Modules_do_not_depend_on_other_modules_internals()
    {
        foreach (var module in Modules.Internals)
        {
            foreach (var other in Modules.Internals.Where(a => a != module))
            {
                Types().That().ResideInAssembly(module)
                    .Should().NotDependOnAny(Types().That().ResideInAssembly(other))
                    .Because("rule 1: use the other module's Contracts project instead")
                    .Check(Modules.Architecture);
            }
        }
    }

    /// <summary>Rule 1: Contracts are the public surface and never expose module internals.</summary>
    [TestMethod]
    public void Contracts_do_not_depend_on_any_module_internals()
    {
        foreach (var contracts in Modules.Contracts)
        {
            foreach (var module in Modules.Internals)
            {
                Types().That().ResideInAssembly(contracts)
                    .Should().NotDependOnAny(Types().That().ResideInAssembly(module))
                    .Because("rule 1: Contracts hold only DTOs, interfaces and integration events")
                    .WithoutRequiringPositiveResults()
                    .Check(Modules.Architecture);
            }
        }
    }

    /// <summary>
    /// Rule 2: the Domain layer has no I/O, no web or EF dependencies, and does not
    /// reach into its own module's outer layers or into other modules.
    /// The ban on reading the system clock is enforced by BannedApiAnalyzers at build time.
    /// </summary>
    [TestMethod]
    public void Domain_depends_only_on_itself_and_the_shared_kernel()
    {
        foreach (var name in Modules.Names)
        {
            var root = Regex.Escape(Modules.RootNamespace(name));
            string[] forbidden =
            [
                $@"^{root}\.(Application|Infrastructure|Endpoints)(\..*)?$",
                .. Modules.Names.Where(n => n != name).Select(n => $@"^{Regex.Escape(Modules.RootNamespace(n))}(\..*)?$"),
                @"^Microsoft\.EntityFrameworkCore(\..*)?$",
                @"^Npgsql(\..*)?$",
                @"^Microsoft\.AspNetCore(\..*)?$",
                @"^System\.IO(\..*)?$",
                @"^System\.Net(\..*)?$",
            ];

            foreach (var pattern in forbidden)
            {
                Types().That().ResideInNamespaceMatching($@"^{root}\.Domain(\..*)?$")
                    .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(pattern)
                    .Because("rule 2: Domain is pure and depends only on the shared kernel")
                    .WithoutRequiringPositiveResults()
                    .Check(Modules.Architecture);
            }
        }
    }
}
