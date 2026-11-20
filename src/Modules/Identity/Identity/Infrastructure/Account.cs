using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// A login account and the person's name and email (data-model.md, IDENTITY).
/// Membership details belong to the Members module, which links here by account id.
/// </summary>
internal sealed class Account : IdentityUser<Guid>
{
    public const int NameMaxLength = 100;

    public required string FirstName { get; set; }

    public required string LastName { get; set; }
}
