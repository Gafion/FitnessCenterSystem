using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>Answers other modules' questions about members. The name comes from Identity (data-model.md).</summary>
internal sealed class MemberDirectory(MembersDbContext db, IAccountService accounts) : IMemberDirectory
{
    public async Task<MemberAtEntrance?> FindAsync(Guid memberId, Instant at, CancellationToken cancellationToken)
    {
        var id = new MemberId(memberId);
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var firstName = await accounts.FindFirstNameAsync(new AccountId(member.AccountId), cancellationToken)
            ?? throw new InvalidOperationException($"Member {memberId} has no account.");
        var payment = member.PaymentStandingAt(at, CenterTime.Zone) switch
        {
            PaymentStanding.Paid => EntrancePayment.Paid,
            PaymentStanding.Missing => EntrancePayment.Missing,
            PaymentStanding.NeverPaid => EntrancePayment.NeverPaid,
            var other => throw new InvalidOperationException($"Unknown payment standing {other}."),
        };
        return new MemberAtEntrance(memberId, firstName, payment);
    }

    public async Task<Guid?> FindMemberIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Members
            .Where(m => m.AccountId == accountId)
            .Select(m => (Guid?)m.Id.Value)
            .SingleOrDefaultAsync(cancellationToken);
}
