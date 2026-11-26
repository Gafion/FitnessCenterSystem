using System.Globalization;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>
/// A class the center cancelled, as the email to a booked member (D-068) or to a member on its
/// waitlist (D-069) describes it.
/// </summary>
internal sealed record CancelledClassNotice(Guid AccountId, string ClassName, Instant StartsAt, bool WasWaitlisted);

/// <summary>A class the center moved, as the email to a booked member describes it (D-068).</summary>
internal sealed record MovedClassNotice(Guid AccountId, string ClassName, Instant MovedFrom, Instant StartsAt);

/// <summary>
/// Tells booked members by email that the center cancelled or moved their class (D-068), and
/// waitlisted members that it cancelled the class (D-069), in the
/// account's language (D-065). Sent after the change is saved. The classes page and the
/// account page show the change too, so a failed send is logged, not retried, as for offers (D-067).
/// </summary>
internal sealed class ClassChangeEmails(IAccountService accounts, ILogger<ClassChangeEmails> logger)
{
    public const string CancelledSubject = "Your class is cancelled";

    public const string CancelledSubjectDanish = "Dit hold er aflyst";

    public const string WaitlistCancelledSubject = "A class you were waiting for is cancelled";

    public const string WaitlistCancelledSubjectDanish = "Et hold, du ventede på, er aflyst";

    public const string MovedSubject = "Your class has moved";

    public const string MovedSubjectDanish = "Dit hold er flyttet";

    private static readonly LocalDateTimePattern ClassTimePattern =
        LocalDateTimePattern.Create("d MMMM uuuu 'at' HH:mm", CultureInfo.InvariantCulture);

    private static readonly LocalDateTimePattern DanishClassTimePattern =
        LocalDateTimePattern.Create("dd.MM.uuuu 'kl.' HH:mm", CultureInfo.InvariantCulture);

    public Task SendAsync(IReadOnlyList<CancelledClassNotice> notices) =>
        SendAllAsync(notices, notice => notice.AccountId, Email);

    public Task SendAsync(IReadOnlyList<MovedClassNotice> notices) =>
        SendAllAsync(notices, notice => notice.AccountId, Email);

    internal static (string Subject, string Body) Email(EmailRecipient recipient, CancelledClassNotice notice)
    {
        var startsAt = notice.StartsAt.InZone(CenterTime.Zone).LocalDateTime;
        if (notice.WasWaitlisted)
        {
            return WaitlistEmail(recipient, notice.ClassName, startsAt);
        }

        if (recipient.Language == Language.English)
        {
            return (CancelledSubject, $"""
                Hi {recipient.FirstName},

                We have cancelled {notice.ClassName}, {ClassTimePattern.Format(startsAt)}, so your booking is cancelled too.
                You don't need to do anything. You can book another class on the classes page.
                """);
        }

        return (CancelledSubjectDanish, $"""
            Hej {recipient.FirstName},

            Vi har aflyst {notice.ClassName}, {DanishClassTimePattern.Format(startsAt)}, så din booking er også aflyst.
            Du behøver ikke gøre noget. Du kan booke et andet hold på holdsiden.
            """);
    }

    private static (string Subject, string Body) WaitlistEmail(
        EmailRecipient recipient, string className, LocalDateTime startsAt)
    {
        if (recipient.Language == Language.English)
        {
            return (WaitlistCancelledSubject, $"""
                Hi {recipient.FirstName},

                We have cancelled {className}, {ClassTimePattern.Format(startsAt)}, so you are no longer on its waitlist.
                You don't need to do anything. You can book another class on the classes page.
                """);
        }

        return (WaitlistCancelledSubjectDanish, $"""
            Hej {recipient.FirstName},

            Vi har aflyst {className}, {DanishClassTimePattern.Format(startsAt)}, så du står ikke længere på ventelisten.
            Du behøver ikke gøre noget. Du kan booke et andet hold på holdsiden.
            """);
    }

    internal static (string Subject, string Body) Email(EmailRecipient recipient, MovedClassNotice notice)
    {
        var from = notice.MovedFrom.InZone(CenterTime.Zone).LocalDateTime;
        var to = notice.StartsAt.InZone(CenterTime.Zone).LocalDateTime;
        if (recipient.Language == Language.English)
        {
            return (MovedSubject, $"""
                Hi {recipient.FirstName},

                {notice.ClassName} has moved from {ClassTimePattern.Format(from)} to {ClassTimePattern.Format(to)}. Your booking is kept.
                If the new time doesn't suit you, you can cancel your booking on your account page until the class starts, even after the usual 12-hour deadline.
                """);
        }

        return (MovedSubjectDanish, $"""
            Hej {recipient.FirstName},

            {notice.ClassName} er flyttet fra {DanishClassTimePattern.Format(from)} til {DanishClassTimePattern.Format(to)}. Din booking er bevaret.
            Passer det nye tidspunkt dig ikke, kan du afmelde dig på din kontoside, indtil holdet starter, også efter den sædvanlige frist på 12 timer.
            """);
    }

    private async Task SendAllAsync<TNotice>(
        IReadOnlyList<TNotice> notices,
        Func<TNotice, Guid> accountOf,
        Func<EmailRecipient, TNotice, (string Subject, string Body)> email)
    {
        foreach (var notice in notices)
        {
            var accountId = new AccountId(accountOf(notice));
            try
            {
                // The change is saved; a client that disconnects now must not stop the email.
                var recipient = await accounts.FindEmailRecipientAsync(accountId, CancellationToken.None)
                    ?? throw new InvalidOperationException($"Account {accountId.Value} does not exist.");
                var (subject, body) = email(recipient, notice);
                await accounts.SendEmailAsync(accountId, subject, body, CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Could not send a class change email to account {AccountId}", accountId.Value);
            }
        }
    }
}
