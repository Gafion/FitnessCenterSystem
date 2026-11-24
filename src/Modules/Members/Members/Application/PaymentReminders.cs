using System.Globalization;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>
/// Sends the one reminder email for each missing payment (D-020, D-042). <see cref="PaymentReminderJob"/>
/// runs it on a timer; it reads the time only from the injected clock, so tests can move the day.
/// </summary>
internal sealed class PaymentReminders(
    MembersDbContext db, IAccountService accounts, IClock clock, ILogger<PaymentReminders> logger)
{
    public const string Subject = "Your payment is missing";

    private static readonly LocalDatePattern DatePattern = LocalDatePattern.Create("d MMMM uuuu", CultureInfo.InvariantCulture);

    /// <summary>
    /// Emails every member whose payment is missing today (Copenhagen) and who hasn't had a
    /// reminder for this paid-until date. A job that was down catches up on its next run.
    /// </summary>
    /// <returns>How many reminders were sent.</returns>
    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetCurrentInstant();
        var latestMissing = MissingPayment.LatestPaidUntilMissingOn(now.InZone(CenterTime.Zone).Date);
        var due = await db.Members.AsNoTracking()
            .Where(m => m.PaidUntil != null && m.PaidUntil <= latestMissing)
            .Where(m => !db.PaymentReminders.Any(r => r.MemberId == m.Id && r.PaidUntil == m.PaidUntil))
            .Select(m => new { m.Id, m.AccountId, PaidUntil = m.PaidUntil!.Value })
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var member in due)
        {
            if (await SendOneAsync(member.Id, member.AccountId, member.PaidUntil, now, cancellationToken))
            {
                sent++;
            }
        }

        return sent;
    }

    /// <summary>
    /// Claims the reminder, sends it, then records that it went out. The claim is a unique row
    /// (member, paid-until), so two jobs at once or a restart never send it twice (at most once).
    /// If sending fails, the claim is removed and the next run tries again.
    /// </summary>
    private async Task<bool> SendOneAsync(
        MemberId memberId, Guid accountId, LocalDate paidUntil, Instant now, CancellationToken cancellationToken)
    {
        var reminderId = PaymentReminderId.New();
        var claimed = await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO members.payment_reminders ("Id", "MemberId", "PaidUntil", "ClaimedAt")
            VALUES ({reminderId.Value}, {memberId.Value}, {paidUntil}, {now})
            ON CONFLICT ("MemberId", "PaidUntil") DO NOTHING
            """,
            cancellationToken);
        if (claimed == 0)
        {
            return false;
        }

        try
        {
            var firstName = await accounts.FindFirstNameAsync(new AccountId(accountId), cancellationToken);
            await accounts.SendEmailAsync(new AccountId(accountId), Subject, Body(firstName, paidUntil), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Payment reminder {ReminderId} could not be sent; the next run tries again.", reminderId.Value);
            await db.Database.ExecuteSqlAsync(
                $"""DELETE FROM members.payment_reminders WHERE "Id" = {reminderId.Value}""",
                CancellationToken.None);
            return false;
        }

        await db.Database.ExecuteSqlAsync(
            $"""UPDATE members.payment_reminders SET "SentAt" = {clock.GetCurrentInstant()} WHERE "Id" = {reminderId.Value}""",
            CancellationToken.None);
        return true;
    }

    private static string Body(string? firstName, LocalDate paidUntil) =>
        $"""
        Hi {firstName},

        We haven't received the payment for your membership after {DatePattern.Format(paidUntil)}.
        Until it arrives, the card reader at the entrance won't let you in.

        If you have already paid, please talk to the staff at the center.
        """;
}
