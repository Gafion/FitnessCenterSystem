using FitnessCenter.Modules.Identity.Contracts;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// How the four staff roles (D-049) relate. Roles stack (D-062): Admin includes Manager, and
/// Manager includes Front desk. Instructor stands apart: only the Instructor role admits it.
/// Who may give which role in an invite is D-063.
/// </summary>
internal static class StaffRoleRules
{
    /// <summary>The roles that a role's policy admits: the role itself and every role above it.</summary>
    public static IReadOnlyList<string> AdmittedBy(string role) => role switch
    {
        StaffRoles.Admin => [StaffRoles.Admin],
        StaffRoles.Manager => [StaffRoles.Admin, StaffRoles.Manager],
        StaffRoles.FrontDesk => [StaffRoles.Admin, StaffRoles.Manager, StaffRoles.FrontDesk],
        StaffRoles.Instructor => [StaffRoles.Instructor],
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Not a staff role."),
    };

    /// <summary>
    /// Whether someone with <paramref name="inviterRoles"/> may invite to <paramref name="role"/>
    /// (D-063): an admin may give any role; a manager only Front desk and Instructor.
    /// </summary>
    public static bool MayGive(IReadOnlyCollection<string> inviterRoles, string role) =>
        inviterRoles.Contains(StaffRoles.Admin)
        || (inviterRoles.Contains(StaffRoles.Manager) && role is StaffRoles.FrontDesk or StaffRoles.Instructor);
}
