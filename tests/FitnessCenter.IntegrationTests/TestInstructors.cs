using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Instructor accounts for the classes the tests schedule: every class needs one (D-060), and
/// the foreign key wants a real account. Synthetic names on example.com (CLAUDE.md).
/// </summary>
internal static class TestInstructors
{
    /// <returns>The new instructor's account ID.</returns>
    public static async Task<Guid> CreateAsync(
        IServiceProvider services, string firstName = "Test", string lastName = "Instructor")
    {
        var email = $"instructor-{Guid.NewGuid():N}@example.com";
        var users = services.GetRequiredService<UserManager<Account>>();
        var account = new Account
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };
        var created = await users.CreateAsync(account);
        Assert.IsTrue(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Code)));
        var role = await users.AddToRoleAsync(account, StaffRoles.Instructor);
        Assert.IsTrue(role.Succeeded, string.Join(", ", role.Errors.Select(e => e.Code)));
        return account.Id;
    }
}
