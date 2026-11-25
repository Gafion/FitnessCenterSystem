using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>Joining and leaving the waitlist of a full class over HTTP (D-051).</summary>
[TestClass]
public sealed class WaitlistTests
{
    [TestMethod]
    public async Task Members_join_a_full_class_in_order_and_leave_it()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, capacity: 1);
        using var booked = await LoggedInClientAsync(factory);
        using var first = await LoggedInClientAsync(factory);
        using var second = await LoggedInClientAsync(factory);
        using (var book = await booked.PostAsync(BookingUri(classId), null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        }

        using (var join = await first.PostAsync(WaitlistUri(classId), null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, join.StatusCode, await join.Content.ReadAsStringAsync());
        }

        using (var join = await second.PostAsync(WaitlistUri(classId), null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, join.StatusCode, await join.Content.ReadAsStringAsync());
        }

        Assert.AreEqual(1, (await FindAsync(first, classId)).MyWaitlistPlace);
        Assert.AreEqual(2, (await FindAsync(second, classId)).MyWaitlistPlace);
        Assert.IsNull((await FindAsync(booked, classId)).MyWaitlistPlace);

        using (var leave = await first.DeleteAsync(WaitlistUri(classId)))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, leave.StatusCode, await leave.Content.ReadAsStringAsync());
        }

        Assert.IsNull((await FindAsync(first, classId)).MyWaitlistPlace);
        Assert.AreEqual(1, (await FindAsync(second, classId)).MyWaitlistPlace);
    }

    [TestMethod]
    public async Task Joining_twice_is_refused()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleFullClassAsync(factory);
        using var client = await LoggedInClientAsync(factory);
        using (await client.PostAsync(WaitlistUri(classId), null))
        {
        }

        using var again = await client.PostAsync(WaitlistUri(classId), null);

        Assert.AreEqual(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Contains("already-waitlisted", await again.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task A_booked_member_cannot_join_the_waitlist()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, capacity: 1);
        using var client = await LoggedInClientAsync(factory);
        using (await client.PostAsync(BookingUri(classId), null))
        {
        }

        using var join = await client.PostAsync(WaitlistUri(classId), null);

        Assert.AreEqual(HttpStatusCode.Conflict, join.StatusCode);
        Assert.Contains("already-booked", await join.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task A_class_with_free_spots_has_no_waitlist()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, capacity: 2);
        using var client = await LoggedInClientAsync(factory);

        using var join = await client.PostAsync(WaitlistUri(classId), null);

        Assert.AreEqual(HttpStatusCode.Conflict, join.StatusCode);
        Assert.Contains("class-not-full", await join.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Booking_a_freed_spot_takes_the_member_off_the_waitlist()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, capacity: 1);
        using var booked = await LoggedInClientAsync(factory);
        using var waiting = await LoggedInClientAsync(factory);
        using (await booked.PostAsync(BookingUri(classId), null))
        {
        }

        using (await waiting.PostAsync(WaitlistUri(classId), null))
        {
        }

        using (await booked.DeleteAsync(BookingUri(classId)))
        {
        }

        using var book = await waiting.PostAsync(BookingUri(classId), null);

        Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        var entry = await FindAsync(waiting, classId);
        Assert.IsTrue(entry.BookedByMe);
        Assert.IsNull(entry.MyWaitlistPlace);
    }

    [TestMethod]
    public async Task Leaving_a_waitlist_one_is_not_on_is_not_found()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleFullClassAsync(factory);
        using var client = await LoggedInClientAsync(factory);

        using var leave = await client.DeleteAsync(WaitlistUri(classId));

        Assert.AreEqual(HttpStatusCode.NotFound, leave.StatusCode);
    }

    [TestMethod]
    public async Task The_waitlist_needs_a_login()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleFullClassAsync(factory);
        using var client = factory.CreateClient();

        using var join = await client.PostAsync(WaitlistUri(classId), null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, join.StatusCode);
    }

    /// <summary>
    /// Many members join the last places at once: the class row lock (D-039) gives each a
    /// distinct place, 1 to n, with no gaps.
    /// </summary>
    [TestMethod]
    public async Task Members_joining_at_once_each_get_their_own_place()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleFullClassAsync(factory);
        var clients = new List<HttpClient>();
        try
        {
            for (var i = 0; i < 5; i++)
            {
                clients.Add(await LoggedInClientAsync(factory));
            }

            var joins = await Task.WhenAll(clients.Select(c => c.PostAsync(WaitlistUri(classId), null)));
            Assert.IsTrue(joins.All(r => r.StatusCode == HttpStatusCode.NoContent));
            joins.ToList().ForEach(r => r.Dispose());

            var places = new List<int?>();
            foreach (var client in clients)
            {
                places.Add((await FindAsync(client, classId)).MyWaitlistPlace);
            }

            CollectionAssert.AreEquivalent(new int?[] { 1, 2, 3, 4, 5 }, places);
        }
        finally
        {
            clients.ForEach(c => c.Dispose());
        }
    }

    private static async Task<HttpClient> LoggedInClientAsync(ApiFactory factory)
    {
        var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        return client;
    }

    /// <summary>Puts a Spinning class two days ahead in the database; there are no staff screens yet (D-022).</summary>
    private static async Task<Guid> ScheduleClassAsync(ApiFactory factory, int capacity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var session = ClassSession.Schedule(
            ClassType.Spinning, "Test Instructor", clock.GetCurrentInstant() + Duration.FromDays(2), Duration.FromMinutes(45), capacity);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.Value;
    }

    /// <summary>A one-spot class already booked by another synthetic account.</summary>
    private static async Task<Guid> ScheduleFullClassAsync(ApiFactory factory)
    {
        var classId = await ScheduleClassAsync(factory, capacity: 1);
        using var other = await LoggedInClientAsync(factory);
        using var book = await other.PostAsync(BookingUri(classId), null);
        Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        return classId;
    }

    private static Uri BookingUri(Guid classId) => new($"/api/booking/classes/{classId}/booking", UriKind.Relative);

    private static Uri WaitlistUri(Guid classId) => new($"/api/booking/classes/{classId}/waitlist", UriKind.Relative);

    private static async Task<ScheduleEntry> FindAsync(HttpClient client, Guid classId)
    {
        var schedule = await client.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule");
        return schedule!.Single(entry => entry.ClassId == classId);
    }

    private sealed record ScheduleEntry(Guid ClassId, int SpotsLeft, bool BookedByMe, int? MyWaitlistPlace);
}
