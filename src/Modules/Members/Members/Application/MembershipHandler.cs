using System.Globalization;
using System.Transactions;
using FitnessCenter.Modules.Booking.Contracts;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>The account's membership as the account page shows it (D-022).</summary>
internal sealed record MembershipSummary(Member Member, MembershipType MembershipType);

/// <summary>
/// The result of a cancellation; the membership as it stands afterwards, and the class
/// bookings after the last day of access that were cancelled with it (D-055 amendment).
/// </summary>
internal sealed record CancellationResult(
    CancellationOutcome Outcome, MembershipSummary Membership, IReadOnlyList<CancelledClassBooking> CancelledBookings);

/// <summary>The result of undoing a cancellation; the membership as it stands afterwards.</summary>
internal sealed record UndoCancellationResult(UndoCancellationOutcome Outcome, MembershipSummary Membership);

/// <summary>
/// Reads, cancels and un-cancels the logged-in account's membership (D-022, D-048, D-055).
/// Cancelling and undoing each send a confirmation email after the change is saved (D-055).
/// Cancelling also cancels the member's class bookings after the last day of access, in the
/// same transaction (D-055 amendment, D-057).
/// </summary>
internal sealed class MembershipHandler(
    MembersDbContext db,
    IAccountService accounts,
    IMemberBookings bookings,
    IClock clock,
    ILogger<MembershipHandler> logger)
{
    public const string CancelledSubject = "Your membership is cancelled";

    public const string UndoneSubject = "Your membership continues";

    // UNVERIFIED: the developer's Danish cancellation wording, for the owner to check (D-065).
    public const string CancelledSubjectDanish = "Dit medlemskab er opsagt";

    public const string UndoneSubjectDanish = "Dit medlemskab fortsætter";

    private static readonly LocalDatePattern DatePattern = LocalDatePattern.Create("d MMMM uuuu", CultureInfo.InvariantCulture);

    private static readonly LocalDateTimePattern ClassTimePattern =
        LocalDateTimePattern.Create("d MMMM uuuu 'at' HH:mm", CultureInfo.InvariantCulture);

    // Danish formats (D-065): 23.11.2026 and 18.00.
    private static readonly LocalDatePattern DanishDatePattern = LocalDatePattern.Create("dd.MM.uuuu", CultureInfo.InvariantCulture);

    private static readonly LocalDateTimePattern DanishClassTimePattern =
        LocalDateTimePattern.Create("dd.MM.uuuu 'kl.' HH.mm", CultureInfo.InvariantCulture);

    /// <summary>Today in Copenhagen, where the undo rule applies (D-055).</summary>
    public LocalDate Today => clock.GetCurrentInstant().InZone(CenterTime.Zone).Date;

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
        Member? member;
        CancellationOutcome outcome;
        IReadOnlyList<CancelledClassBooking> cancelledBookings = [];

        // Every module's DbContext uses the request's shared connection (D-015), so this
        // transaction covers the cancellation and the bookings Booking cancels. If either part
        // throws, neither is saved: no cancelled membership with its bookings left behind.
        using (var transaction = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled))
        {
            member = await db.Members.SingleOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);
            if (member is null)
            {
                return null;
            }

            var now = clock.GetCurrentInstant();
            outcome = member.Cancel(now, CenterTime.Zone);
            if (outcome == CancellationOutcome.Cancelled)
            {
                await db.SaveChangesAsync(cancellationToken);
                cancelledBookings = await bookings.CancelBookingsAfterAsync(
                    member.AccountId, member.CancellationEffectiveOn!.Value, now, cancellationToken);
                transaction.Complete();
            }
        }

        if (outcome == CancellationOutcome.Cancelled)
        {
            var lastDay = member.CancellationEffectiveOn!.Value;
            await SendAfterCommitAsync(member.AccountId, recipient => CancelledEmail(recipient, lastDay, cancelledBookings));
        }

        var membershipType = await db.MembershipTypes.AsNoTracking()
            .SingleAsync(t => t.Id == member.MembershipTypeId, cancellationToken);
        return new CancellationResult(outcome, new MembershipSummary(member, membershipType), cancelledBookings);
    }

    private static (string Subject, string Body) CancelledEmail(
        EmailRecipient recipient, LocalDate lastDay, IReadOnlyList<CancelledClassBooking> cancelled)
    {
        if (recipient.Language == Language.English)
        {
            var effectiveOn = DatePattern.Format(lastDay);
            return (CancelledSubject, $"""
                Hi {recipient.FirstName},

                We have received your cancellation. Your membership ends after {effectiveOn}.
                You can use the center up to and including that day.
                {BookingsText(cancelled, effectiveOn)}
                Changed your mind? You can undo the cancellation on your account page until {effectiveOn}.
                """);
        }

        var sidsteDag = DanishDatePattern.Format(lastDay);
        return (CancelledSubjectDanish, $"""
            Hej {recipient.FirstName},

            Vi har modtaget din opsigelse. Dit medlemskab slutter efter {sidsteDag}.
            Du kan bruge centret til og med den dag.
            {DanishBookingsText(cancelled, sidsteDag)}
            Har du fortrudt? Du kan annullere opsigelsen på din kontoside til og med {sidsteDag}.
            """);
    }

    /// <summary>
    /// The email's lines about cancelled class bookings (D-055 amendment): how many, and which.
    /// Empty when no bookings were cancelled. Undoing doesn't book them again.
    /// </summary>
    private static string BookingsText(IReadOnlyList<CancelledClassBooking> cancelled, string effectiveOn)
    {
        if (cancelled.Count == 0)
        {
            return "";
        }

        var count = cancelled.Count == 1 ? "1 class booking" : $"{cancelled.Count} class bookings";
        var lines = cancelled.Select(booking =>
            $"- {booking.ClassName}, {ClassTimePattern.Format(booking.StartsAt.InZone(CenterTime.Zone).LocalDateTime)}");
        return $"""

            We have cancelled your {count} after {effectiveOn}, so others can take the spots:
            {string.Join("\n", lines)}
            If you undo the cancellation, these bookings are not restored. You can book them again.

            """;
    }

    private static string DanishBookingsText(IReadOnlyList<CancelledClassBooking> cancelled, string lastDay)
    {
        if (cancelled.Count == 0)
        {
            return "";
        }

        var count = cancelled.Count == 1 ? "din holdtilmelding" : $"dine {cancelled.Count} holdtilmeldinger";
        var lines = cancelled.Select(booking =>
            $"- {booking.ClassName}, {DanishClassTimePattern.Format(booking.StartsAt.InZone(CenterTime.Zone).LocalDateTime)}");
        return $"""

            Vi har afmeldt {count} efter {lastDay}, så andre kan få pladserne:
            {string.Join("\n", lines)}
            Hvis du annullerer opsigelsen, kommer tilmeldingerne ikke tilbage. Du kan tilmelde dig igen.

            """;
    }

    /// <summary>
    /// Undoes the account's pending cancellation, up to and including its effective date
    /// (D-055). With nothing pending, nothing changes and no email is sent.
    /// </summary>
    /// <returns>The outcome, or null when the account has no membership.</returns>
    public async Task<UndoCancellationResult?> UndoCancellationAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var member = await db.Members.SingleOrDefaultAsync(m => m.AccountId == accountId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var outcome = member.UndoCancellation(clock.GetCurrentInstant(), CenterTime.Zone);
        if (outcome == UndoCancellationOutcome.Undone)
        {
            await db.SaveChangesAsync(cancellationToken);
            await SendAfterCommitAsync(member.AccountId, recipient => recipient.Language == Language.English
                ? (UndoneSubject, $"""
                    Hi {recipient.FirstName},

                    You have undone your cancellation. Your membership continues as before.
                    """)
                : (UndoneSubjectDanish, $"""
                    Hej {recipient.FirstName},

                    Du har annulleret din opsigelse. Dit medlemskab fortsætter som før.
                    """));
        }

        var membershipType = await db.MembershipTypes.AsNoTracking()
            .SingleAsync(t => t.Id == member.MembershipTypeId, cancellationToken);
        return new UndoCancellationResult(outcome, new MembershipSummary(member, membershipType));
    }

    /// <summary>
    /// Sends a confirmation email once the change is saved. A failed send is logged and does
    /// not reverse the change; the account page still shows it (D-055).
    /// </summary>
    private async Task SendAfterCommitAsync(Guid accountId, Func<EmailRecipient, (string Subject, string Body)> email)
    {
        try
        {
            // The change is saved; a client that disconnects now must not stop the email.
            // The email is in the account's language (D-065).
            var recipient = await accounts.FindEmailRecipientAsync(new AccountId(accountId), CancellationToken.None)
                ?? throw new InvalidOperationException($"Account {accountId} does not exist.");
            var (subject, body) = email(recipient);
            await accounts.SendEmailAsync(new AccountId(accountId), subject, body, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not send a membership email to account {AccountId}", accountId);
        }
    }
}
