using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>The account's membership as the account page shows it (D-022).</summary>
internal sealed record MembershipSummary(Member Member, MembershipType MembershipType);

/// <summary>The result of a cancellation; the membership as it stands afterwards.</summary>
internal sealed record CancellationResult(CancellationOutcome Outcome, MembershipSummary Membership);

/// <summary>Reads and cancels the logged-in account's membership (D-022, D-048).</summary>
internal sealed class MembershipHandler(MembersDbContext db, IClock clock)
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

    /// <summary>
    /// Cancels the account's membership now, with one month's notice to the end of a month
    /// (D-048). A second cancellation leaves the first one as it was.
    /// </summary>
    /// <returns>The outcome, or null when the account has no membership.</returns>
    public async Task<CancellationResult?> CancelAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var member = await db.Members.SingleOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var outcome = member.Cancel(clock.GetCurrentInstant(), CenterTime.Zone);
        if (outcome == CancellationOutcome.Cancelled)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var membershipType = await db.MembershipTypes.AsNoTracking()
            .SingleAsync(t => t.Id == member.MembershipTypeId, cancellationToken);
        return new CancellationResult(outcome, new MembershipSummary(member, membershipType));
    }
}
