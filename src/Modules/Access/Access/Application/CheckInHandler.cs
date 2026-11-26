using FitnessCenter.Modules.Access.Domain;
using FitnessCenter.Modules.Access.Infrastructure;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Contracts;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Access.Application;

internal enum CheckInOutcome
{
    /// <summary>The card belongs to a member; an attendance is recorded.</summary>
    Welcome,

    /// <summary>No card has this UID. Nothing is recorded.</summary>
    UnknownCard,

    /// <summary>
    /// The card's member has a missing payment (D-020, D-042) or has never paid (D-046). Check-in
    /// is refused and nothing is recorded: an attendance is an accepted swipe (data-model.md ATTENDANCE).
    /// </summary>
    PaymentMissing,

    /// <summary>
    /// The member cancelled and the cancellation has taken effect (D-048, D-055). Check-in is
    /// refused and nothing is recorded.
    /// </summary>
    MembershipEnded,
}

/// <param name="FirstName">The member's first name, for the door screen. Only set on <see cref="CheckInOutcome.Welcome"/>.</param>
internal sealed record CheckInResult(CheckInOutcome Outcome, string? FirstName, Instant At);

/// <summary>
/// The check-in logic behind every card reader: the simulated one now, the real one later (D-003, D-021).
/// </summary>
internal sealed class CheckInHandler(AccessDbContext db, IMemberDirectory members, IClock clock, IAuditLog audit)
{
    /// <summary>Audit trail actions (D-072). The actor is the member whose card was swiped.</summary>
    public const string CheckedIn = "checked-in";

    /// <summary>Detail: <c>membership-ended</c> or <c>payment-missing</c>.</summary>
    public const string CheckInRefused = "check-in-refused";

    public async Task<CheckInResult> CheckInAsync(string? cardUid, CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var uid = CardUid.Normalize(cardUid);
        var card = uid is null ? null : await db.AccessCards.SingleOrDefaultAsync(c => c.Uid == uid, cancellationToken);
        if (card is null)
        {
            return new CheckInResult(CheckInOutcome.UnknownCard, null, now);
        }

        var member = await members.FindAsync(card.MemberId, now, cancellationToken)
            ?? throw new InvalidOperationException($"Card {card.Id.Value} belongs to a member that doesn't exist.");

        // D-048, D-055: access lasts until the cancellation takes effect, and not after. Checked
        // first: once the membership has ended, a payment can't fix it. No name, as below.
        if (member.MembershipEnded)
        {
            await audit.RecordAsync(member.AccountId, member.AccountId, CheckInRefused, "membership-ended", cancellationToken);
            return new CheckInResult(CheckInOutcome.MembershipEnded, null, now);
        }

        // D-020, D-042: a missing payment refuses check-in, before anything is recorded. D-046: so
        // does never having paid. No name is returned: the door screen is public, and the warning
        // shouldn't say who is behind.
        if (member.Payment is EntrancePayment.Missing or EntrancePayment.NeverPaid)
        {
            await audit.RecordAsync(member.AccountId, member.AccountId, CheckInRefused, "payment-missing", cancellationToken);
            return new CheckInResult(CheckInOutcome.PaymentMissing, null, now);
        }

        db.Attendances.Add(card.Swipe(now));
        await db.SaveChangesAsync(cancellationToken);
        await audit.RecordAsync(member.AccountId, member.AccountId, CheckedIn, null, cancellationToken);
        return new CheckInResult(CheckInOutcome.Welcome, member.FirstName, now);
    }

    /// <returns>The UID of the card that belongs to this account's member, or null when there is none.</returns>
    public async Task<string?> FindCardUidAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var memberId = await members.FindMemberIdAsync(accountId, cancellationToken);
        return memberId is null
            ? null
            : await db.AccessCards
                .Where(c => c.MemberId == memberId.Value)
                .Select(c => c.Uid)
                .SingleOrDefaultAsync(cancellationToken);
    }
}
