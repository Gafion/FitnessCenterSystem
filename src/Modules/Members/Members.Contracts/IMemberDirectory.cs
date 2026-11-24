using NodaTime;

namespace FitnessCenter.Modules.Members.Contracts;

/// <summary>
/// What other modules may ask the Members module about a member (rule 1).
/// Access uses it at check-in (D-003).
/// </summary>
public interface IMemberDirectory
{
    /// <param name="at">The moment of the check-in; the payment answer is for that Copenhagen day (D-042).</param>
    /// <returns>The member, or null when there is no such member.</returns>
    Task<MemberAtEntrance?> FindAsync(Guid memberId, Instant at, CancellationToken cancellationToken);

    /// <returns>
    /// The last Copenhagen day of access of this Identity account's member when they have
    /// cancelled (D-048): the cancellation takes effect at its end. Null when the account isn't
    /// a member or the membership isn't cancelled. Booking uses it (D-055).
    /// </returns>
    Task<LocalDate?> FindLastDayOfAccessAsync(Guid accountId, CancellationToken cancellationToken);

    /// <returns>The member that signs in with this Identity account, or null when it isn't a member.</returns>
    Task<Guid?> FindMemberIdAsync(Guid accountId, CancellationToken cancellationToken);
}

/// <summary>What the entrance needs to know about a member, including whether they have paid (D-020).</summary>
/// <param name="MembershipEnded">
/// The member cancelled and the cancellation has taken effect before the check-in's Copenhagen
/// day (D-048, D-055). Check-in is refused.
/// </param>
public sealed record MemberAtEntrance(Guid MemberId, string FirstName, EntrancePayment Payment, bool MembershipEnded);

/// <summary>The member's payments as the entrance sees them (D-020, D-042).</summary>
public enum EntrancePayment
{
    /// <summary>The day is covered by a payment.</summary>
    Paid,

    /// <summary>The paid-until date has passed, with no grace period (D-042). Check-in is refused.</summary>
    Missing,

    /// <summary>
    /// The member has never paid (AwaitingFirstPayment). Check-in is refused like a missing
    /// payment (D-046), but no reminder email is sent (D-046 leaves that open).
    /// </summary>
    NeverPaid,
}
