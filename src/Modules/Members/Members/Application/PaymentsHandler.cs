using FitnessCenter.Modules.Members.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>A member's payments, newest period first, and how far they are paid (D-020).</summary>
internal sealed record PaymentHistory(
    MemberStatus Status, LocalDate? PaidUntil, Money MonthlyPrice, IReadOnlyList<PaymentRecord> Payments);

/// <summary>The result of a simulated payment; <see cref="Payment"/> is set when it was recorded.</summary>
internal sealed record SimulatedPaymentResult(PaymentConfirmationResult Result, PaymentRecord? Payment);

/// <summary>Payment history and the payment simulator (D-020, D-021, D-041).</summary>
internal sealed class PaymentsHandler(MembersDbContext db, IPaymentConfirmations confirmations, IClock clock)
{
    /// <summary>Synthetic card digits for simulated payments; never a real card (CLAUDE.md).</summary>
    public const string SimulatedCardLast4 = "4242";

    /// <returns>The history, or null when the account has no membership.</returns>
    public async Task<PaymentHistory?> GetHistoryAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var membershipType = await db.MembershipTypes.AsNoTracking()
            .SingleAsync(t => t.Id == member.MembershipTypeId, cancellationToken);
        var payments = await db.PaymentRecords.AsNoTracking()
            .Where(p => p.MemberId == member.Id)
            .OrderByDescending(p => p.PeriodFrom)
            .ThenByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);
        return new PaymentHistory(member.Status, member.PaidUntil, membershipType.MonthlyPrice, payments);
    }

    /// <summary>
    /// Sends a payment confirmation for the account's member, as the Payments service will:
    /// one month at the membership price, from <paramref name="periodFrom"/> or else from
    /// the day after paid-until (D-021). A past period lets the missing-payment rule be tried.
    /// </summary>
    /// <returns>The result, or null when the account has no membership.</returns>
    public async Task<SimulatedPaymentResult?> SimulateAsync(
        Guid accountId, LocalDate? periodFrom, CancellationToken cancellationToken)
    {
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var membershipType = await db.MembershipTypes.AsNoTracking()
            .SingleAsync(t => t.Id == member.MembershipTypeId, cancellationToken);
        var now = clock.GetCurrentInstant();
        var period = PaymentPeriod.MonthFrom(periodFrom ?? member.NextPeriodStart(now.InZone(CenterTime.Zone).Date));
        var paymentRef = $"simulated-{Guid.CreateVersion7()}";

        var result = await confirmations.ReceiveAsync(
            new PaymentConfirmed(
                paymentRef,
                member.Id.Value,
                membershipType.MonthlyPrice.Ore,
                now,
                SimulatedCardLast4,
                period.From,
                period.To),
            cancellationToken);

        var payment = result == PaymentConfirmationResult.Recorded
            ? await db.PaymentRecords.AsNoTracking().SingleAsync(p => p.PaymentRef == paymentRef, cancellationToken)
            : null;
        return new SimulatedPaymentResult(result, payment);
    }
}
