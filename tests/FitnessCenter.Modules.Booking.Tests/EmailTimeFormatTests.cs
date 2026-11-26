using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Booking.Tests;

/// <summary>
/// Times in the class emails use a colon, 18:00, in Danish and in English (D-070), and a
/// cancelled class tells waitlisted members they are off the waitlist (D-069). Synthetic names only.
/// </summary>
[TestClass]
public sealed class EmailTimeFormatTests
{
    // Tuesday 12 January 2027, 18:00 in Copenhagen (UTC+1).
    private static readonly Instant Start = Instant.FromUtc(2027, 1, 12, 17, 0);

    // Wednesday 13 January 2027, 19:30 in Copenhagen.
    private static readonly Instant NewStart = Instant.FromUtc(2027, 1, 13, 18, 30);

    private static readonly Guid Account = Guid.Parse("0199a3c0-0000-7000-8000-00000000c001");
    private static readonly EmailRecipient English = new("Ann", Language.English);
    private static readonly EmailRecipient Danish = new("Ann", Language.Danish);

    [TestMethod]
    public void A_cancelled_booking_email_uses_a_colon_in_both_languages()
    {
        var notice = new CancelledClassNotice(Account, "Spinning", Start, WasWaitlisted: false);

        var (subject, body) = ClassChangeEmails.Email(English, notice);
        var (emne, tekst) = ClassChangeEmails.Email(Danish, notice);

        Assert.AreEqual(ClassChangeEmails.CancelledSubject, subject);
        StringAssert.Contains(body, "We have cancelled Spinning, 12 January 2027 at 18:00, so your booking is cancelled too.");
        Assert.AreEqual(ClassChangeEmails.CancelledSubjectDanish, emne);
        StringAssert.Contains(tekst, "Vi har aflyst Spinning, 12.01.2027 kl. 18:00, så din booking er også aflyst.");
    }

    [TestMethod]
    public void A_waitlisted_member_is_told_the_class_is_cancelled_in_their_language()
    {
        var notice = new CancelledClassNotice(Account, "Spinning", Start, WasWaitlisted: true);

        var (subject, body) = ClassChangeEmails.Email(English, notice);
        var (emne, tekst) = ClassChangeEmails.Email(Danish, notice);

        Assert.AreEqual(ClassChangeEmails.WaitlistCancelledSubject, subject);
        StringAssert.Contains(body, "Hi Ann,");
        StringAssert.Contains(body, "We have cancelled Spinning, 12 January 2027 at 18:00, so you are no longer on its waitlist.");
        Assert.AreEqual(ClassChangeEmails.WaitlistCancelledSubjectDanish, emne);
        StringAssert.Contains(tekst, "Hej Ann,");
        StringAssert.Contains(tekst, "Vi har aflyst Spinning, 12.01.2027 kl. 18:00, så du står ikke længere på ventelisten.");
    }

    [TestMethod]
    public void A_moved_class_email_uses_a_colon_in_both_languages()
    {
        var notice = new MovedClassNotice(Account, "Spinning", Start, NewStart);

        var (_, body) = ClassChangeEmails.Email(English, notice);
        var (_, tekst) = ClassChangeEmails.Email(Danish, notice);

        StringAssert.Contains(body, "Spinning has moved from 12 January 2027 at 18:00 to 13 January 2027 at 19:30.");
        StringAssert.Contains(tekst, "Spinning er flyttet fra 12.01.2027 kl. 18:00 til 13.01.2027 kl. 19:30.");
    }

    [TestMethod]
    public void An_offer_email_uses_a_colon_in_both_languages()
    {
        // Held until 14:00 the same day, or until 16:30 the day before the class.
        var sameDay = new NewOffer(Account, "Spinning", Start, Start - Duration.FromHours(4));
        var dayBefore = new NewOffer(Account, "Spinning", Start, Start - Duration.FromMinutes(90) - Duration.FromDays(1));

        StringAssert.Contains(OfferEmails.Email(English, sameDay).Body, "Spinning, 12 January 2027 at 18:00");
        StringAssert.Contains(OfferEmails.Email(English, sameDay).Body, "by 14:00.");
        StringAssert.Contains(OfferEmails.Email(Danish, sameDay).Body, "Spinning, 12.01.2027 kl. 18:00");
        StringAssert.Contains(OfferEmails.Email(Danish, sameDay).Body, "senest kl. 14:00.");
        StringAssert.Contains(OfferEmails.Email(English, dayBefore).Body, "by 11 January 2027 at 16:30.");
        StringAssert.Contains(OfferEmails.Email(Danish, dayBefore).Body, "senest 11.01.2027 kl. 16:30.");
    }
}
