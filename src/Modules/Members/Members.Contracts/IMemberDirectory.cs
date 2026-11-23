namespace FitnessCenter.Modules.Members.Contracts;

/// <summary>
/// What other modules may ask the Members module about a member (rule 1).
/// Access uses it at check-in (D-003).
/// </summary>
public interface IMemberDirectory
{
    /// <returns>The member, or null when there is no such member.</returns>
    Task<MemberAtEntrance?> FindAsync(Guid memberId, CancellationToken cancellationToken);

    /// <returns>The member that signs in with this Identity account, or null when it isn't a member.</returns>
    Task<Guid?> FindMemberIdAsync(Guid accountId, CancellationToken cancellationToken);
}

/// <summary>
/// What the entrance needs to know about a member. FIT-45 adds whether the member has paid,
/// so check-in can refuse a missing payment (D-020).
/// </summary>
public sealed record MemberAtEntrance(Guid MemberId, string FirstName);
