using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>The account's membership as the account page shows it (D-022).</summary>
internal sealed record MembershipSummary(Member Member, MembershipType MembershipType);

/// <summary>Reads the logged-in account's membership.</summary>
internal sealed class MembershipHandler(MembersDbContext db)
{
    /// <returns>The membership, or null when the account has none.</returns>
    public async Task<MembershipSummary?> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var membershipType = await db.MembershipTypes.AsNoTracking()
            .SingleAsync(t => t.Id == member.MembershipTypeId, cancellationToken);
        return new MembershipSummary(member, membershipType);
    }
}
