namespace FitnessCenter.Modules.Identity.Contracts;

/// <summary>
/// The four staff roles (D-049). Members have none of them. Single site, so no site scopes (D-001).
/// What each role may do comes with each staff feature.
/// </summary>
public static class StaffRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string FrontDesk = "FrontDesk";
    public const string Instructor = "Instructor";

    public static IReadOnlyList<string> All { get; } = [Admin, Manager, FrontDesk, Instructor];
}

/// <summary>
/// Authorization policies that endpoints in any module can require, e.g.
/// <c>.RequireAuthorization(StaffPolicies.FrontDesk)</c>. Roles stack (D-062): the Manager
/// policy also admits Admin, and the FrontDesk policy admits Manager and Admin. Instructor
/// admits the Instructor role only; an admin or manager is not an instructor without that role.
/// </summary>
public static class StaffPolicies
{
    public const string Admin = "Staff.Admin";
    public const string Manager = "Staff.Manager";
    public const string FrontDesk = "Staff.FrontDesk";
    public const string Instructor = "Staff.Instructor";

    /// <summary>Any of the four staff roles.</summary>
    public const string AnyStaff = "Staff.Any";
}
