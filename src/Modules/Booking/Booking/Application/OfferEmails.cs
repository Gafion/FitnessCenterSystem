using System.Globalization;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>A spot offered to a member from the waitlist, as the email describes it (D-067).</summary>
internal sealed record NewOffer(Guid AccountId, string ClassName, Instant StartsAt, Instant ExpiresAt);

/// <summary>
/// Tells a member by email that a spot is held for them (D-067), in the account's language
/// (D-065). The account page shows the same offer, so a failed send is logged, not retried.
/// </summary>
internal sealed class OfferEmails(IAccountService accounts, ILogger<OfferEmails> logger)
{
    public const string Subject = "A spot is free for you";

    public const string SubjectDanish = "Der er en plads til dig";

    private static readonly LocalDateTimePattern ClassTimePattern =
        LocalDateTimePattern.Create("d MMMM uuuu 'at' HH:mm", CultureInfo.InvariantCulture);

    private static readonly LocalDateTimePattern DanishClassTimePattern =
        LocalDateTimePattern.Create("dd.MM.uuuu 'kl.' HH.mm", CultureInfo.InvariantCulture);

    private static readonly LocalTimePattern TimePattern = LocalTimePattern.Create("HH:mm", CultureInfo.InvariantCulture);

    private static readonly LocalTimePattern DanishTimePattern = LocalTimePattern.Create("HH.mm", CultureInfo.InvariantCulture);

    /// <summary>Sends one email per offer. Call it after the offers are saved.</summary>
    public async Task SendAsync(IReadOnlyList<NewOffer> offers)
    {
        foreach (var offer in offers)
        {
            try
            {
                // The offer is saved; a client that disconnects now must not stop the email.
                var recipient = await accounts.FindEmailRecipientAsync(new AccountId(offer.AccountId), CancellationToken.None)
                    ?? throw new InvalidOperationException($"Account {offer.AccountId} does not exist.");
                var (subject, body) = Email(recipient, offer);
                await accounts.SendEmailAsync(new AccountId(offer.AccountId), subject, body, CancellationToken.None);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Could not send a waitlist offer email to account {AccountId}", offer.AccountId);
            }
        }
    }

    internal static (string Subject, string Body) Email(EmailRecipient recipient, NewOffer offer)
    {
        var startsAt = offer.StartsAt.InZone(CenterTime.Zone).LocalDateTime;
        var expiresAt = offer.ExpiresAt.InZone(CenterTime.Zone).LocalDateTime;
        var sameDay = startsAt.Date == expiresAt.Date;
        if (recipient.Language == Language.English)
        {
            var until = sameDay ? TimePattern.Format(expiresAt.TimeOfDay) : ClassTimePattern.Format(expiresAt);
            return (Subject, $"""
                Hi {recipient.FirstName},

                A spot has freed up in {offer.ClassName}, {ClassTimePattern.Format(startsAt)}, and it is held for you.
                Accept it on your account page by {until}. After that, it goes to the next person on the waitlist.
                If you can't come, decline it there, so the next person gets it sooner.
                """);
        }

        var indtil = sameDay ? $"kl. {DanishTimePattern.Format(expiresAt.TimeOfDay)}" : DanishClassTimePattern.Format(expiresAt);
        return (SubjectDanish, $"""
            Hej {recipient.FirstName},

            Der er blevet en plads fri på {offer.ClassName}, {DanishClassTimePattern.Format(startsAt)}, og den er holdt til dig.
            Tag imod den på din kontoside senest {indtil}. Derefter går den til den næste på ventelisten.
            Kan du ikke komme, så afslå den dér, så den næste får den hurtigere.
            """);
    }
}
