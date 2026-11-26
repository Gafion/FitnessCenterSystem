using FitnessCenter.Modules.Booking.Contracts;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
using FitnessCenter.Modules.Members.Application;
using NodaTime;

namespace FitnessCenter.Modules.Members.Tests;

/// <summary>
/// The membership cancellation email lists cancelled class bookings with a colon in the time,
/// 18:00, in Danish and in English (D-070). Synthetic names only.
/// </summary>
[TestClass]
public sealed class CancellationEmailTimeFormatTests
{
    private static readonly LocalDate LastDay = new(2027, 4, 30);

    // Saturday 1 May 2027, 18:00 in Copenhagen (UTC+2).
    private static readonly CancelledClassBooking Booking = new("Spinning", Instant.FromUtc(2027, 5, 1, 16, 0));

    [TestMethod]
    public void Cancelled_bookings_use_a_colon_in_both_languages()
    {
        var (_, body) = MembershipHandler.CancelledEmail(new EmailRecipient("Ann", Language.English), LastDay, [Booking]);
        var (_, tekst) = MembershipHandler.CancelledEmail(new EmailRecipient("Ann", Language.Danish), LastDay, [Booking]);

        StringAssert.Contains(body, "- Spinning, 1 May 2027 at 18:00");
        StringAssert.Contains(tekst, "- Spinning, 01.05.2027 kl. 18:00");
    }
}
