using System.Security.Cryptography;
using FitnessCenter.Modules.Access.Contracts;
using FitnessCenter.Modules.Access.Domain;
using FitnessCenter.Modules.Access.Infrastructure;

namespace FitnessCenter.Modules.Access.Application;

/// <summary>Issues simulated cards (D-021). Real cards wait for the hardware decision (FIT-33).</summary>
internal sealed class CardIssuer(AccessDbContext db) : IAccessCards
{
    public async Task IssueCardAsync(Guid memberId, CancellationToken cancellationToken)
    {
        // 56 random bits: a clash is so unlikely that the unique index is enough of a guard.
        var uid = CardUid.Simulated(RandomNumberGenerator.GetBytes(7));
        db.AccessCards.Add(AccessCard.Issue(memberId, uid));
        await db.SaveChangesAsync(cancellationToken);
    }
}
