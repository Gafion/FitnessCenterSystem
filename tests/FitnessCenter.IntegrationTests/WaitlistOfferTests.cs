using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// A freed spot is offered to the next on the waitlist, who is emailed and accepts or declines
/// it (D-051, D-067). The clock is fixed and moved by hand; the offer sweep (WaitlistOfferJob)
/// is off in tests, so the tests run it themselves.
/// </summary>
[TestClass]
public sealed class WaitlistOfferTests
{
    // Monday 11 January 2027, 10:00 in Copenhagen (UTC+1).
    private static readonly Instant Now = Instant.FromUtc(2027, 1, 11, 9, 0);

    // Tuesday 12 January 2027, 18:00 in Copenhagen; the cancel deadline is 06:00 that day.
    private static readonly Instant ClassStart = Instant.FromUtc(2027, 1, 12, 17, 0);

    // The sweep goes through every class in the shared test database, on its own test's clock.
    // The test that runs it uses classes in 2031, so it can't expire other tests' offers, and
    // other tests' sweeps (on earlier clocks) can't expire its offers either.
    private static readonly Instant SweepNow = Instant.FromUtc(2031, 1, 13, 9, 0);
    private static readonly Instant SweepClassStart = Instant.FromUtc(2031, 1, 14, 17, 0);

    [TestMethod]
    public async Task Cancelling_offers_the_spot_to_the_next_person_who_is_emailed_and_accepts()
    {
        var clock = new MovableClock(Now);
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        var classId = await ScheduleClassAsync(factory);
        using var booked = await LoggedInClientAsync(factory);
        var firstEmail = SyntheticEmail();
        using var first = await LoggedInClientAsync(factory, firstEmail);
        using var second = await LoggedInClientAsync(factory);
        await SucceedsAsync(booked.PostAsync(BookingUri(classId), null));
        await SucceedsAsync(first.PostAsync(WaitlistUri(classId), null));
        await SucceedsAsync(second.PostAsync(WaitlistUri(classId), null));

        await SucceedsAsync(booked.DeleteAsync(BookingUri(classId)));

        var text = await TestMailbox.TextToAsync(firstEmail, OfferEmails.Subject);
        StringAssert.Contains(text, "A spot has freed up in Spinning, 12 January 2027 at 18:00, and it is held for you.");
        StringAssert.Contains(text, "Accept it on your account page by 11 January 2027 at 12:00.");

        var offer = (await first.GetFromJsonAsync<MyOffer[]>("/api/booking/my-offers"))!.Single();
        Assert.AreEqual(classId, offer.ClassId);
        Assert.AreEqual(Now.Plus(Duration.FromHours(2)).ToDateTimeOffset(), offer.ExpiresAt);
        Assert.AreEqual(offer.ExpiresAt, (await FindAsync(first, classId)).MyOfferExpiresAt);
        Assert.AreEqual(0, (await FindAsync(second, classId)).SpotsLeft, "the held spot is not free for others");
        Assert.IsEmpty((await second.GetFromJsonAsync<MyOffer[]>("/api/booking/my-offers"))!);

        clock.Now = Now + Duration.FromMinutes(30);
        await SucceedsAsync(first.PostAsync(OfferUri(classId), null));

        var schedule = await FindAsync(first, classId);
        Assert.IsTrue(schedule.BookedByMe);
        Assert.IsNull(schedule.MyOfferExpiresAt);
        Assert.IsNull(schedule.MyWaitlistPlace);
        Assert.IsEmpty((await first.GetFromJsonAsync<MyOffer[]>("/api/booking/my-offers"))!);
        Assert.AreEqual(1, (await FindAsync(second, classId)).MyWaitlistPlace);
    }

    /// <summary>The sweep expires an unanswered offer and offers the spot on, in the next person's language (D-065).</summary>
    [TestMethod]
    public async Task An_unanswered_offer_expires_and_the_next_person_gets_it_in_their_language()
    {
        var clock = new MovableClock(SweepNow);
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        var classId = await ScheduleClassAsync(factory, SweepClassStart);
        using var booked = await LoggedInClientAsync(factory);
        using var first = await LoggedInClientAsync(factory);
        var danishEmail = SyntheticEmail();
        using var danish = await LoggedInClientAsync(factory, danishEmail, danish: true);
        await SucceedsAsync(booked.PostAsync(BookingUri(classId), null));
        await SucceedsAsync(first.PostAsync(WaitlistUri(classId), null));
        await SucceedsAsync(danish.PostAsync(WaitlistUri(classId), null));
        await SucceedsAsync(booked.DeleteAsync(BookingUri(classId)));

        clock.Now = SweepNow + Duration.FromHours(2);
        await SweepAsync(factory);

        using var late = await first.PostAsync(OfferUri(classId), null);
        Assert.AreEqual(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Contains("offer-expired", await late.Content.ReadAsStringAsync());
        Assert.IsFalse((await FindAsync(first, classId)).BookedByMe);

        var text = await TestMailbox.TextToAsync(danishEmail, OfferEmails.SubjectDanish);
        StringAssert.Contains(text, "Der er blevet en plads fri på Spinning, 14.01.2031 kl. 18.00");
        StringAssert.Contains(text, "senest 13.01.2031 kl. 14.00");
        Assert.AreEqual(
            (SweepNow + Duration.FromHours(4)).ToDateTimeOffset(),
            (await FindAsync(danish, classId)).MyOfferExpiresAt);

        await SweepAsync(factory);
        Assert.AreEqual(1, await TestMailbox.CountToAsync(danishEmail, OfferEmails.SubjectDanish), "a second sweep offers nothing new");
    }

    [TestMethod]
    public async Task Declining_offers_the_spot_to_the_next_person_at_once()
    {
        var clock = new MovableClock(Now);
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        var classId = await ScheduleClassAsync(factory);
        using var booked = await LoggedInClientAsync(factory);
        using var first = await LoggedInClientAsync(factory);
        var secondEmail = SyntheticEmail();
        using var second = await LoggedInClientAsync(factory, secondEmail);
        await SucceedsAsync(booked.PostAsync(BookingUri(classId), null));
        await SucceedsAsync(first.PostAsync(WaitlistUri(classId), null));
        await SucceedsAsync(second.PostAsync(WaitlistUri(classId), null));
        await SucceedsAsync(booked.DeleteAsync(BookingUri(classId)));

        clock.Now = Now + Duration.FromMinutes(10);
        await SucceedsAsync(first.DeleteAsync(OfferUri(classId)));

        Assert.IsNull((await FindAsync(first, classId)).MyWaitlistPlace);
        Assert.AreEqual(
            (Now + Duration.FromMinutes(130)).ToDateTimeOffset(),
            (await FindAsync(second, classId)).MyOfferExpiresAt);
        Assert.AreEqual(1, await TestMailbox.CountToAsync(secondEmail, OfferEmails.Subject));
        using var again = await first.DeleteAsync(OfferUri(classId));
        Assert.AreEqual(HttpStatusCode.NotFound, again.StatusCode);
    }

    [TestMethod]
    public async Task Accepting_without_an_offer_is_not_found()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        var classId = await ScheduleClassAsync(factory);
        using var client = await LoggedInClientAsync(factory);

        using var accept = await client.PostAsync(OfferUri(classId), null);

        Assert.AreEqual(HttpStatusCode.NotFound, accept.StatusCode);
    }

    [TestMethod]
    public async Task Offers_need_a_login()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var offers = await client.GetAsync("/api/booking/my-offers");

        Assert.AreEqual(HttpStatusCode.Unauthorized, offers.StatusCode);
    }

    private static async Task SweepAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<BookingHandler>().UpdateOffersAsync(CancellationToken.None);
    }

    private static async Task SucceedsAsync(Task<HttpResponseMessage> request)
    {
        using var response = await request;
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    /// <param name="danish">Sign up without Accept-Language, so the account's language is Danish (D-065).</param>
    private static async Task<HttpClient> LoggedInClientAsync(ApiFactory factory, string? email = null, bool danish = false)
    {
        var client = factory.CreateClient();
        if (danish)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
        }

        await SignUpConfirmedAndLoggedInAsync(client, email ?? SyntheticEmail());
        return client;
    }

    /// <summary>A one-spot Spinning class; there are no staff screens yet (D-022).</summary>
    private static async Task<Guid> ScheduleClassAsync(ApiFactory factory, Instant? startsAt = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var session = ClassSession.Schedule(ClassType.Spinning, "Test Instructor", startsAt ?? ClassStart, Duration.FromMinutes(45), 1);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.Value;
    }

    private static Uri BookingUri(Guid classId) => new($"/api/booking/classes/{classId}/booking", UriKind.Relative);

    private static Uri WaitlistUri(Guid classId) => new($"/api/booking/classes/{classId}/waitlist", UriKind.Relative);

    private static Uri OfferUri(Guid classId) => new($"/api/booking/classes/{classId}/offer", UriKind.Relative);

    private static async Task<ScheduleEntry> FindAsync(HttpClient client, Guid classId)
    {
        var schedule = await client.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule");
        return schedule!.Single(entry => entry.ClassId == classId);
    }

    private sealed record ScheduleEntry(
        Guid ClassId, int SpotsLeft, bool BookedByMe, int? MyWaitlistPlace, DateTimeOffset? MyOfferExpiresAt);

    private sealed record MyOffer(Guid ClassId, string ClassName, DateTimeOffset StartsAt, DateTimeOffset ExpiresAt);

    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
