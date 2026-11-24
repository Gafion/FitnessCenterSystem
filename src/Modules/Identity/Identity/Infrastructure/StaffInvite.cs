using NodaTime;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// An admin's invitation for one person to become staff with one role (D-058). The link's
/// secret code is not stored, only its SHA-256 hash, so a database leak gives no working links.
/// </summary>
internal sealed class StaffInvite
{
    /// <summary>Hex of a SHA-256 hash.</summary>
    public const int CodeHashLength = 64;

    public Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Role { get; init; }

    public required string CodeHash { get; init; }

    /// <summary>The admin who sent the invite.</summary>
    public Guid InvitedBy { get; init; }

    public Instant CreatedAt { get; init; }

    public Instant ExpiresAt { get; init; }

    /// <summary>Set when the invite is used. A used invite can't be used again.</summary>
    public Instant? AcceptedAt { get; set; }

    /// <summary>The staff account the invite created.</summary>
    public Guid? AccountId { get; set; }
}
