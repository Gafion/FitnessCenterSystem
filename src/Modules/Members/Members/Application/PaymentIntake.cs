using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>
/// Records payment confirmations and moves the member's paid-until date (D-020). The single
/// entry point for the simulator now and the Payments service later (D-021).
/// </summary>
internal sealed class PaymentIntake(MembersDbContext db, IAuditLog audit) : IPaymentConfirmations
{
    public async Task<PaymentConfirmationResult> ReceiveAsync(PaymentConfirmed confirmation, CancellationToken cancellationToken)
    {
        var memberId = new MemberId(confirmation.MemberId);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Lock the member row first, so two confirmations for one member take turns: the
        // second sees the first one's paid-until date and payment ref.
        await db.Database.ExecuteSqlAsync(
            $"""SELECT 1 FROM members.members WHERE "Id" = {memberId.Value} FOR UPDATE""",
            cancellationToken);

        var member = await db.Members.SingleOrDefaultAsync(m => m.Id == memberId, cancellationToken);
        if (member is null)
        {
            return PaymentConfirmationResult.UnknownMember;
        }

        // Confirmations arrive at least once (architecture.md §13), so a repeat is not an error.
        if (await db.PaymentRecords.AnyAsync(p => p.PaymentRef == confirmation.PaymentRef, cancellationToken))
        {
            return PaymentConfirmationResult.AlreadyRecorded;
        }

        var period = new PaymentPeriod(confirmation.PeriodFrom, confirmation.PeriodTo);
        if (member.ApplyPayment(period) == PaymentOutcome.NotContiguous)
        {
            return PaymentConfirmationResult.PeriodNotContiguous;
        }

        db.PaymentRecords.Add(new PaymentRecord(
            PaymentRecordId.New(),
            member.Id,
            confirmation.PaymentRef,
            new Money(confirmation.AmountOre),
            confirmation.PaidAt,
            confirmation.CardLast4,
            period));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await audit.RecordAsync(null, member.AccountId, AuditActions.PaymentRecorded, null, cancellationToken);
        return PaymentConfirmationResult.Recorded;
    }
}
