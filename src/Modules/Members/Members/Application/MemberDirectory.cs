using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>Answers other modules' questions about members. The name comes from Identity (data-model.md).</summary>
internal sealed class MemberDirectory(MembersDbContext db, IAccountService accounts) : IMemberDirectory
{
    public async Task<MemberAtEntrance?> FindAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var id = new MemberId(memberId);
        var accountId = await db.Members
            .Where(m => m.Id == id)
            .Select(m => (Guid?)m.AccountId)
            .SingleOrDefaultAsync(cancellationToken);
        if (accountId is null)
        {
            return null;
        }

        var firstName = await accounts.FindFirstNameAsync(new AccountId(accountId.Value), cancellationToken)
            ?? throw new InvalidOperationException($"Member {memberId} has no account.");
        return new MemberAtEntrance(memberId, firstName);
    }

    public async Task<Guid?> FindMemberIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Members
            .Where(m => m.AccountId == accountId)
            .Select(m => (Guid?)m.Id.Value)
            .SingleOrDefaultAsync(cancellationToken);
}
