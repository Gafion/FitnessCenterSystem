using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Contracts;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Members.Application;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Cancelling a membership cancels the member's bookings in classes after the last day of
/// access, in the same transaction (D-055 amendment, D-057, FIT-97). D-048's example:
/// cancelled on 10 March 2027, the cancellation takes effect at the end of 30 April.
/// Copenhagen is UTC+2 in April and May.
/// </summary>
/// <remarks>
/// Today the booking window (14 days, D-039) is shorter than the notice (at least a month,
/// D-048), so a member can't hold such a booking through the API when they cancel. The tests
/// put the bookings in through the domain, as if the window were longer.
/// </remarks>
[TestClass]
public sealed class MembershipEndBookingsTests
{
    // Wednesday 10 March 2027, 13:00 in Copenhagen (UTC+1).
    private static readonly Instant CancelledAt = Instant.FromUtc(2027, 3, 10, 12, 0);

    // 18:00 in Copenhagen on the effective date, and on the two days after it.
    private static readonly Instant OnEffectiveDate = Instant.FromUtc(2027, 4, 30, 16, 0);
    private static readonly Instant DayAfter = Instant.FromUtc(2027, 5, 1, 16, 0);
    private static readonly Instant TwoDaysAfter = Instant.FromUtc(2027, 5, 2, 16, 0);

    [TestMethod]
    public async Task Cancelling_cancels_the_bookings_after_the_effective_date_and_frees_the_spots()
    {
        var clock = new MovableClock(CancelledAt);
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        using var leaving = factory.CreateClient();
        using var staying = factory.CreateClient();
        var leavingEmail = SyntheticEmail();
        var leaver = await AccountOfAsync(factory, await SignUpConfirmedAndLoggedInAsync(leaving, leavingEmail));
        var stayer = await AccountOfAsync(factory, await SignUpConfirmedAndLoggedInAsync(staying, SyntheticEmail()));
        var onEffectiveDate = await ScheduleClassAsync(factory, OnEffectiveDate, capacity: 5, leaver);
        var dayAfter = await ScheduleClassAsync(factory, DayAfter, capacity: 1, leaver);
        var shared = await ScheduleClassAsync(factory, TwoDaysAfter, capacity: 5, leaver, stayer);

        using var cancel = await leaving.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());

        Assert.IsTrue(await IsBookedAsync(factory, onEffectiveDate, leaver), "a booking on the effective date stays");
        Assert.IsFalse(await IsBookedAsync(factory, dayAfter, leaver), "a booking the day after is cancelled");
        Assert.IsFalse(await IsBookedAsync(factory, shared, leaver));
        Assert.IsTrue(await IsBookedAsync(factory, shared, stayer), "other members' bookings are untouched");

        var text = await TestMailbox.TextToAsync(leavingEmail, MembershipHandler.CancelledSubject);
        StringAssert.Contains(text, "We have cancelled your 2 class bookings after 30 April 2027");
        StringAssert.Contains(text, "- Spinning, 1 May 2027 at 18:00");
        StringAssert.Contains(text, "- Spinning, 2 May 2027 at 18:00");
        StringAssert.Contains(text, "these bookings are not restored");

        // The day-after class had one spot, held by the leaver. Someone else can take it now.
        clock.Now = Instant.FromUtc(2027, 4, 28, 10, 0);
        using var book = await staying.PostAsync(BookingUri(dayAfter), null);
        Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        Assert.IsTrue(await IsBookedAsync(factory, dayAfter, stayer));
    }

    [TestMethod]
    public async Task Cancelling_without_bookings_after_the_effective_date_mentions_none()
    {
        var email = SyntheticEmail();
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(CancelledAt)));
        using var client = factory.CreateClient();
        var account = await AccountOfAsync(factory, await SignUpConfirmedAndLoggedInAsync(client, email));
        var onEffectiveDate = await ScheduleClassAsync(factory, OnEffectiveDate, capacity: 5, account);

        using var cancel = await client.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());

        Assert.IsTrue(await IsBookedAsync(factory, onEffectiveDate, account));
        var text = await TestMailbox.TextToAsync(email, MembershipHandler.CancelledSubject);
        StringAssert.Contains(text, "ends after 30 April 2027");
        Assert.IsFalse(text.Contains("class booking", StringComparison.Ordinal), text);
    }

    /// <summary>D-057: if the booking step fails, the cancellation is not saved either.</summary>
    [TestMethod]
    public async Task A_failure_in_the_booking_step_rolls_back_the_cancellation()
    {
        var email = SyntheticEmail();
        await using var factory = new ApiFactory(services =>
        {
            services.AddSingleton<IClock>(new MovableClock(CancelledAt));
            // The real step runs and cancels the booking, then fails: both must roll back.
            services.AddScoped<IMemberBookings>(provider =>
                new FailingAfterwards(ActivatorUtilities.CreateInstance<MemberBookings>(provider)));
        });
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, email);
        var account = await AccountOfAsync(factory, memberId);
        var dayAfter = await ScheduleClassAsync(factory, DayAfter, capacity: 5, account);

        using var cancel = await client.PostAsync("/api/members/me/cancellation", null);

        Assert.AreEqual(HttpStatusCode.InternalServerError, cancel.StatusCode);
        Assert.IsTrue(await IsBookedAsync(factory, dayAfter, account), "the booking is not cancelled");
        await using var scope = factory.Services.CreateAsyncScope();
        var members = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
        var member = await members.Members.AsNoTracking().SingleAsync(m => m.Id == new MemberId(memberId));
        Assert.IsNull(member.CancelledAt, "the cancellation is not saved");
        Assert.IsNull(member.CancellationEffectiveOn);
        Assert.AreEqual(0, await TestMailbox.CountToAsync(email, MembershipHandler.CancelledSubject));
    }

    [TestMethod]
    public async Task Undoing_the_cancellation_does_not_restore_the_bookings()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(CancelledAt)));
        using var client = factory.CreateClient();
        var account = await AccountOfAsync(factory, await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail()));
        var dayAfter = await ScheduleClassAsync(factory, DayAfter, capacity: 5, account);
        using var cancel = await client.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());

        using var undo = await client.DeleteAsync("/api/members/me/cancellation");

        Assert.AreEqual(HttpStatusCode.OK, undo.StatusCode, await undo.Content.ReadAsStringAsync());
        Assert.IsFalse(await IsBookedAsync(factory, dayAfter, account), "the member books again");
    }

    private static async Task<Guid> AccountOfAsync(ApiFactory factory, Guid memberId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MembersDbContext>();
        return await db.Members.Where(m => m.Id == new MemberId(memberId)).Select(m => m.AccountId).SingleAsync();
    }

    /// <summary>
    /// Puts a Spinning class in the database with the given accounts booked a day before it
    /// starts; there are no staff screens yet (D-022), and see the class remarks for the window.
    /// </summary>
    private static async Task<ClassSessionId> ScheduleClassAsync(
        ApiFactory factory, Instant startsAt, int capacity, params Guid[] bookedBy)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var session = ClassSession.Schedule(ClassType.Spinning, "Test Instructor", startsAt, Duration.FromMinutes(45), capacity);
        foreach (var account in bookedBy)
        {
            Assert.AreEqual(BookOutcome.Booked, session.Book(account, startsAt - Duration.FromDays(1)));
        }

        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    private static async Task<bool> IsBookedAsync(ApiFactory factory, ClassSessionId sessionId, Guid accountId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var session = await db.ClassSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId);
        return session.IsBookedBy(accountId);
    }

    private static Uri BookingUri(ClassSessionId classId) =>
        new($"/api/booking/classes/{classId.Value}/booking", UriKind.Relative);

    private sealed class FailingAfterwards(IMemberBookings inner) : IMemberBookings
    {
        public async Task<IReadOnlyList<CancelledClassBooking>> CancelBookingsAfterAsync(
            Guid accountId, LocalDate lastDayOfAccess, Instant now, CancellationToken cancellationToken)
        {
            await inner.CancelBookingsAfterAsync(accountId, lastDayOfAccess, now, cancellationToken);
            throw new InvalidOperationException("Simulated failure in the booking step (FIT-97).");
        }
    }

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
