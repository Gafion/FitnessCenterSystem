using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Access.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>Access cards and check-in through the simulated reader (D-003, D-021, D-041).</summary>
[TestClass]
public sealed class AccessTests
{
    private const string MyCardUri = "/api/access/cards/mine";
    private const string ReaderUri = "/api/access/simulation/reader";
    private const string SwipeUri = "/api/access/simulation/reader/swipes";
    private const string SwipeMyCardUri = "/api/access/simulation/reader/swipes/my-card";

    private static readonly Instant Now = Instant.FromUtc(2027, 1, 12, 6, 30);

    [TestMethod]
    public async Task A_member_gets_a_simulated_card_at_signup()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        var card = await client.GetFromJsonAsync<MyCard>(MyCardUri);

        Assert.StartsWith("SIM-", card!.Uid);
    }

    [TestMethod]
    public async Task Members_get_different_cards()
    {
        await using var factory = new ApiFactory();
        using var first = factory.CreateClient();
        using var second = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(first, SyntheticEmail());
        await SignUpConfirmedAndLoggedInAsync(second, SyntheticEmail());

        var firstCard = await first.GetFromJsonAsync<MyCard>(MyCardUri);
        var secondCard = await second.GetFromJsonAsync<MyCard>(MyCardUri);

        Assert.AreNotEqual(firstCard!.Uid, secondCard!.Uid);
    }

    [TestMethod]
    public async Task Swiping_a_card_welcomes_the_member_and_records_an_attendance()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new FixedClock(Now)));
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        var card = await client.GetFromJsonAsync<MyCard>(MyCardUri);

        // Typed by hand, in lower case with spaces: the reader still knows the card.
        using var swipe = await client.PostAsJsonAsync(SwipeUri, new { cardUid = $" {card!.Uid.ToLowerInvariant()} " });

        Assert.AreEqual(HttpStatusCode.OK, swipe.StatusCode, await swipe.Content.ReadAsStringAsync());
        var result = await swipe.Content.ReadFromJsonAsync<SwipeResult>();
        Assert.AreEqual("welcome", result!.Outcome);
        Assert.AreEqual("Test", result.FirstName);
        Assert.AreEqual(Now.ToDateTimeOffset(), result.At);
        Assert.AreEqual(1, await AttendancesAsync(factory, card.Uid, Now));
    }

    [TestMethod]
    public async Task Swiping_my_card_checks_me_in()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new FixedClock(Now)));
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        var card = await client.GetFromJsonAsync<MyCard>(MyCardUri);

        using var swipe = await client.PostAsync(SwipeMyCardUri, null);

        Assert.AreEqual(HttpStatusCode.OK, swipe.StatusCode, await swipe.Content.ReadAsStringAsync());
        var result = await swipe.Content.ReadFromJsonAsync<SwipeResult>();
        Assert.AreEqual("welcome", result!.Outcome);
        Assert.AreEqual("Test", result.FirstName);
        Assert.AreEqual(1, await AttendancesAsync(factory, card!.Uid, Now));
    }

    [TestMethod]
    public async Task An_unknown_card_is_refused_and_nothing_is_recorded()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var before = await db.Attendances.CountAsync();

        using var swipe = await client.PostAsJsonAsync(SwipeUri, new { cardUid = "SIM-NOT-A-REAL-CARD" });

        Assert.AreEqual(HttpStatusCode.OK, swipe.StatusCode, await swipe.Content.ReadAsStringAsync());
        var result = await swipe.Content.ReadFromJsonAsync<SwipeResult>();
        Assert.AreEqual("unknown-card", result!.Outcome);
        Assert.IsNull(result.FirstName);
        Assert.AreEqual(before, await db.Attendances.CountAsync());
    }

    [TestMethod]
    public async Task The_reader_needs_a_login()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var swipe = await client.PostAsJsonAsync(SwipeUri, new { cardUid = "SIM-00000000000000" });
        using var card = await client.GetAsync(MyCardUri);

        Assert.AreEqual(HttpStatusCode.Unauthorized, swipe.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, card.StatusCode);
    }

    [TestMethod]
    public async Task The_simulated_reader_is_there_while_the_simulation_is_on()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var reader = await client.GetAsync(ReaderUri);

        Assert.AreEqual(HttpStatusCode.NoContent, reader.StatusCode);
    }

    /// <summary>D-041: with the setting off, the simulated reader doesn't exist. The card itself still does.</summary>
    [TestMethod]
    public async Task With_the_simulation_off_the_reader_is_not_found()
    {
        await using var factory = new ApiFactory();
        await using var off = factory.WithWebHostBuilder(builder => builder.UseSetting("Simulation:Enabled", "false"));
        using var client = off.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());
        var card = await client.GetFromJsonAsync<MyCard>(MyCardUri);

        using var reader = await client.GetAsync(ReaderUri);
        using var swipe = await client.PostAsJsonAsync(SwipeUri, new { cardUid = card!.Uid });
        using var swipeMine = await client.PostAsync(SwipeMyCardUri, null);

        Assert.AreEqual(HttpStatusCode.NotFound, reader.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, swipe.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, swipeMine.StatusCode);
    }

    /// <summary>D-041: the setting is checked before the login, so nobody can tell the reader exists.</summary>
    [TestMethod]
    public async Task With_the_simulation_off_the_reader_is_not_found_without_a_login_either()
    {
        await using var factory = new ApiFactory();
        await using var off = factory.WithWebHostBuilder(builder => builder.UseSetting("Simulation:Enabled", "false"));
        using var client = off.CreateClient();

        using var swipe = await client.PostAsJsonAsync(SwipeUri, new { cardUid = "SIM-00000000000000" });

        Assert.AreEqual(HttpStatusCode.NotFound, swipe.StatusCode);
    }

    private static async Task<int> AttendancesAsync(ApiFactory factory, string uid, Instant at)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var card = await db.AccessCards.SingleAsync(c => c.Uid == uid);
        return await db.Attendances.CountAsync(a => a.AccessCardId == card.Id && a.MemberId == card.MemberId && a.SwipedAt == at);
    }

    private sealed class FixedClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }

    private sealed record MyCard(string Uid);

    private sealed record SwipeResult(string Outcome, string? FirstName, DateTimeOffset At);
}
