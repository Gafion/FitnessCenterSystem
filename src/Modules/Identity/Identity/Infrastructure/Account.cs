using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
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

    /// <summary>The language of the account's emails (D-065). Danish unless the person chose English.</summary>
    public Language PreferredLanguage { get; set; } = Languages.Default;
}
