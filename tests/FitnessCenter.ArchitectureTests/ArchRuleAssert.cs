using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;

namespace FitnessCenter.ArchitectureTests;

/// <summary>
/// MSTest assertion for ArchUnitNET rules. The official ArchUnitNET.MSTestV2
/// adapter does not load under MSTest 4, so this replaces its Check().
/// </summary>
internal static class ArchRuleAssert
{
    public static void Check(this IArchRule rule, Architecture architecture)
    {
        if (rule.HasNoViolations(architecture))
        {
            return;
        }

        var violations = rule.Evaluate(architecture)
            .Where(result => !result.Passed)
            .Select(result => result.Description);

        Assert.Fail($"{rule.Description}{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }
}
