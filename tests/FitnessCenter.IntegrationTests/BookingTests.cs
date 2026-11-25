using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>Class schedule, booking and cancelling over HTTP (D-022, D-039).</summary>
[TestClass]
public sealed class BookingTests
{
    [TestMethod]
    public async Task Anyone_sees_the_schedule_with_spots_left()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity: 4);
        using var client = factory.CreateClient();

        var entry = await FindAsync(client, classId);

        Assert.AreEqual("Spinning", entry.ClassName);
        Assert.AreEqual(4, entry.SpotsLeft);
        Assert.IsFalse(entry.BookedByMe);
    }

    [TestMethod]
    public async Task Member_books_a_spot_and_cancels_it()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity: 4);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var book = await client.PostAsync(BookingUri(classId), null);
        Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        var booked = await FindAsync(client, classId);
        Assert.IsTrue(booked.BookedByMe);
        Assert.AreEqual(3, booked.SpotsLeft);

        using var cancel = await client.DeleteAsync(BookingUri(classId));
        Assert.AreEqual(HttpStatusCode.NoContent, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());
        var cancelled = await FindAsync(client, classId);
        Assert.IsFalse(cancelled.BookedByMe);
        Assert.AreEqual(4, cancelled.SpotsLeft);
    }

    [TestMethod]
    public async Task Booking_needs_a_login()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity: 4);
        using var client = factory.CreateClient();

        using var book = await client.PostAsync(BookingUri(classId), null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, book.StatusCode);
    }

    [TestMethod]
    public async Task A_full_class_says_so()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity: 1);
        using var first = factory.CreateClient();
        using var second = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(first, SyntheticEmail());
        await SignUpConfirmedAndLoggedInAsync(second, SyntheticEmail());
        using (await first.PostAsync(BookingUri(classId), null))
        {
        }

        using var book = await second.PostAsync(BookingUri(classId), null);

        Assert.AreEqual(HttpStatusCode.Conflict, book.StatusCode);
        Assert.Contains("class-full", await book.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Cancelling_is_closed_12_hours_before_the_start()
    {
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromHours(6), capacity: 4);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        using var book = await client.PostAsync(BookingUri(classId), null);
        Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode);

        using var cancel = await client.DeleteAsync(BookingUri(classId));

        Assert.AreEqual(HttpStatusCode.Conflict, cancel.StatusCode);
        Assert.Contains("cancellation-closed", await cancel.Content.ReadAsStringAsync());
        Assert.IsTrue((await FindAsync(client, classId)).BookedByMe);
    }

    [TestMethod]
    public async Task An_unknown_class_is_not_found()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var book = await client.PostAsync(BookingUri(Guid.NewGuid()), null);

        Assert.AreEqual(HttpStatusCode.NotFound, book.StatusCode);
    }

    [TestMethod]
    public async Task My_bookings_lists_booked_classes_soonest_first_and_drops_cancelled_ones()
    {
        await using var factory = new ApiFactory();
        var later = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(3), capacity: 4);
        var sooner = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(1), capacity: 4);
        var cancelled = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity: 4);
        var notBooked = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity: 4);
        using var client = factory.CreateClient();
        using var other = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await SignUpConfirmedAndLoggedInAsync(other, SyntheticEmail());
        foreach (var classId in new[] { later, sooner, cancelled })
        {
            using var book = await client.PostAsync(BookingUri(classId), null);
            Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        }

        using (await other.PostAsync(BookingUri(notBooked), null))
        {
        }

        using (await client.DeleteAsync(BookingUri(cancelled)))
        {
        }

        var mine = (await client.GetFromJsonAsync<MyBooking[]>("/api/booking/my-bookings"))!;

        CollectionAssert.AreEqual(new[] { sooner, later }, mine.Select(b => b.ClassId).ToArray());
        Assert.AreEqual("Spinning", mine[0].ClassName);
        Assert.AreEqual("Test I.", mine[0].InstructorName, "the instructor account's first name and last initial (D-060)");
        Assert.AreEqual(mine[0].StartsAt.AddHours(-12), mine[0].CancelUntil);
    }

    [TestMethod]
    public async Task My_bookings_leaves_out_classes_that_have_started()
    {
        var clock = new MovableClock(Instant.FromUtc(2026, 11, 23, 12, 0));
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromHours(2), capacity: 4);
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        using (var book = await client.PostAsync(BookingUri(classId), null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode);
        }

        Assert.HasCount(1, (await client.GetFromJsonAsync<MyBooking[]>("/api/booking/my-bookings"))!);
        clock.Now += Duration.FromHours(2);

        Assert.IsEmpty((await client.GetFromJsonAsync<MyBooking[]>("/api/booking/my-bookings"))!);
    }

    [TestMethod]
    public async Task My_bookings_needs_a_login()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/booking/my-bookings");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Many members try for the last spots at the same moment: exactly as many bookings
    /// succeed as there are spots (the row lock in BookingHandler, D-039).
    /// </summary>
    [TestMethod]
    public async Task Simultaneous_bookings_never_overfill_a_class()
    {
        const int capacity = 3;
        const int members = 12;
        await using var factory = new ApiFactory();
        var classId = await ScheduleClassAsync(factory, startsIn: Duration.FromDays(2), capacity);
        var clients = Enumerable.Range(0, members).Select(_ => factory.CreateClient()).ToList();
        try
        {
            await Task.WhenAll(clients.Select(c => SignUpConfirmedAndLoggedInAsync(c, SyntheticEmail())));

            var responses = await Task.WhenAll(clients.Select(c => c.PostAsync(BookingUri(classId), null)));

            var statuses = responses.Select(r => r.StatusCode).ToList();
            Assert.AreEqual(capacity, statuses.Count(s => s == HttpStatusCode.NoContent), string.Join(", ", statuses));
            Assert.AreEqual(members - capacity, statuses.Count(s => s == HttpStatusCode.Conflict), string.Join(", ", statuses));
            Assert.AreEqual(0, (await FindAsync(clients[0], classId)).SpotsLeft);
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
        finally
        {
            clients.ForEach(c => c.Dispose());
        }
    }

    /// <summary>Puts a Spinning class in the database; there are no staff screens yet (D-022).</summary>
    private static async Task<Guid> ScheduleClassAsync(ApiFactory factory, Duration startsIn, int capacity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var session = ClassSession.Schedule(
            ClassType.Spinning, await TestInstructors.CreateAsync(scope.ServiceProvider), clock.GetCurrentInstant() + startsIn, Duration.FromMinutes(45), capacity);
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

    private sealed record ScheduleEntry(Guid ClassId, string ClassName, int SpotsLeft, bool BookedByMe);

    private sealed record MyBooking(
        Guid ClassId, string ClassName, string InstructorName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, DateTimeOffset CancelUntil);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
