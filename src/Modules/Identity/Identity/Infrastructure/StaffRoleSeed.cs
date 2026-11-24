using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// The four staff roles (D-049) as rows in <c>identity.AspNetRoles</c>, created by a migration.
/// The ids and concurrency stamps are fixed so the model doesn't change between builds.
/// </summary>
internal static class StaffRoleSeed
{
    public static IReadOnlyList<IdentityRole<Guid>> Roles { get; } =
    [
        Role(StaffRoles.Admin, "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a01"),
        Role(StaffRoles.Manager, "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a02"),
        Role(StaffRoles.FrontDesk, "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a03"),
        Role(StaffRoles.Instructor, "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a04"),
    ];

    private static IdentityRole<Guid> Role(string name, string id) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };
}
