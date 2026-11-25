using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Access.Infrastructure;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// After a cancellation takes effect the member can't check in, and can't book classes after
/// the last day of access (D-048, D-055). D-048's example: cancelled on 10 March 2027, the
/// cancellation takes effect at the end of 30 April. Copenhagen is UTC+2 in April and May.
/// </summary>
[TestClass]
public sealed class CancellationEffectTests
{
    private const string SwipeMyCardUri = "/api/access/simulation/reader/swipes/my-card";

    // Wednesday 10 March 2027, 13:00 in Copenhagen (UTC+1).
    private static readonly Instant CancelledAt = Instant.FromUtc(2027, 3, 10, 12, 0);

    private static ApiFactory Factory(IClock clock) => new(services => services.AddSingleton(clock));

    [TestMethod]
    [DataRow(2027, 4, 29, 10, 0, 0, "welcome", DisplayName = "the day before the effective date")]
    [DataRow(2027, 4, 30, 21, 59, 59, "welcome", DisplayName = "the last second of the effective date")]
    [DataRow(2027, 4, 30, 22, 0, 0, "membership-ended", DisplayName = "midnight after the effective date")]
    [DataRow(2027, 5, 1, 10, 0, 0, "membership-ended", DisplayName = "the day after the effective date")]
    public async Task Check_in_works_until_the_cancellation_takes_effect(
        int year, int month, int day, int hour, int minute, int second, string expected)
    {
        var clock = new MovableClock(CancelledAt);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        var memberId = await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        // Paid until 31 May, so a refusal can only come from the cancellation.
        await PayAsync(client, new DateOnly(2027, 3, 1));
        await PayAsync(client, new DateOnly(2027, 4, 1));
        await PayAsync(client, new DateOnly(2027, 5, 1));
        await CancelAsync(client);

        clock.Now = Instant.FromUtc(year, month, day, hour, minute, second);
        var result = await SwipeMyCardAsync(client);

        Assert.AreEqual(expected, result.Outcome);
        Assert.AreEqual(expected == "welcome" ? 1 : 0, await AttendancesAsync(factory, memberId), "a refusal records nothing");
        if (expected != "welcome")
        {
            Assert.IsNull(result.FirstName, "the door screen is public, so a refusal doesn't name the member");
        }
    }

    [TestMethod]
    public async Task A_cancelled_member_books_a_class_on_the_effective_date_but_not_the_day_after()
    {
        var clock = new MovableClock(CancelledAt);
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await CancelAsync(client);
        clock.Now = Instant.FromUtc(2027, 4, 28, 10, 0);
        // 18:00 in Copenhagen on 30 April (the effective date) and on 1 May.
        var onEffectiveDate = await ScheduleClassAsync(factory, Instant.FromUtc(2027, 4, 30, 16, 0));
        var dayAfter = await ScheduleClassAsync(factory, Instant.FromUtc(2027, 5, 1, 16, 0));

        Assert.IsFalse((await FindAsync(client, onEffectiveDate)).AfterMyMembershipEnds);
        Assert.IsTrue((await FindAsync(client, dayAfter)).AfterMyMembershipEnds);

        using var allowed = await client.PostAsync(BookingUri(onEffectiveDate), null);
        Assert.AreEqual(HttpStatusCode.NoContent, allowed.StatusCode, await allowed.Content.ReadAsStringAsync());

        using var refused = await client.PostAsync(BookingUri(dayAfter), null);
        Assert.AreEqual(HttpStatusCode.Conflict, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<Problem>();
        Assert.AreEqual("after-membership-ends", problem!.Type);
        var refusedEntry = await FindAsync(client, dayAfter);
        Assert.IsFalse(refusedEntry.BookedByMe);
        Assert.AreEqual(5, refusedEntry.SpotsLeft);
    }

    [TestMethod]
    public async Task A_member_who_has_not_cancelled_books_any_class_in_the_window()
    {
        var clock = new MovableClock(Instant.FromUtc(2027, 4, 28, 10, 0));
        await using var factory = Factory(clock);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        var classId = await ScheduleClassAsync(factory, Instant.FromUtc(2027, 5, 1, 16, 0));

        Assert.IsFalse((await FindAsync(client, classId)).AfterMyMembershipEnds);
        using var book = await client.PostAsync(BookingUri(classId), null);
        Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
    }

    private static async Task CancelAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/api/members/me/cancellation", null);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<SwipeResult> SwipeMyCardAsync(HttpClient client)
    {
        using var swipe = await client.PostAsync(SwipeMyCardUri, null);
        Assert.AreEqual(HttpStatusCode.OK, swipe.StatusCode, await swipe.Content.ReadAsStringAsync());
        return (await swipe.Content.ReadFromJsonAsync<SwipeResult>())!;
    }

    private static async Task<int> AttendancesAsync(ApiFactory factory, Guid memberId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        return await db.Attendances.CountAsync(a => a.MemberId == memberId);
    }

    /// <summary>Puts a Spinning class in the database; there are no staff screens yet (D-022).</summary>
    private static async Task<Guid> ScheduleClassAsync(ApiFactory factory, Instant startsAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var session = ClassSession.Schedule(ClassType.Spinning, await TestInstructors.CreateAsync(scope.ServiceProvider), startsAt, Duration.FromMinutes(45), 5);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.Value;
    }

    private static Uri BookingUri(Guid classId) => new($"/api/booking/classes/{classId}/booking", UriKind.Relative);

    private static async Task<ScheduleEntry> FindAsync(HttpClient client, Guid classId)
    {
        var schedule = await client.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule");
        return schedule!.Single(entry => entry.ClassId == classId);
    }

    private sealed record ScheduleEntry(Guid ClassId, int SpotsLeft, bool BookedByMe, bool AfterMyMembershipEnds);

    private sealed record Problem(string Type);

    private sealed record SwipeResult(string Outcome, string? FirstName, DateTimeOffset At);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
