using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// The instructor's own upcoming classes and who booked them (FIT-106): Instructor policy
/// (D-062), only the instructor's own classes (D-049), booked members' display names only
/// (GDPR minimum). Synthetic accounts only.
/// </summary>
[TestClass]
public sealed class MyClassesTests
{
    private const string Url = "/api/booking/my-classes";

    private static ApiFactory? _factory;

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public static IEnumerable<object[]> NonInstructorRoles =>
        [[StaffRoles.Admin], [StaffRoles.Manager], [StaffRoles.FrontDesk]];

    [ClassInitialize]
    public static void Initialize(TestContext _) => _factory = new ApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Instructor_sees_only_their_own_upcoming_classes_soonest_first()
    {
        var (client, me) = await StaffInviteTests.LoggedInStaffWithIdAsync(Factory, StaffRoles.Instructor);
        using var _ = client;
        var other = await OtherInstructorAsync();
        var later = await ScheduleAsync(me, Duration.FromDays(5), capacity: 8);
        var sooner = await ScheduleAsync(me, Duration.FromDays(1), capacity: 6);
        var notMine = await ScheduleAsync(other, Duration.FromDays(2), capacity: 4);
        var started = await ScheduleAsync(me, Duration.FromMinutes(-10), capacity: 4);

        var classes = (await client.GetFromJsonAsync<MyClass[]>(Url))!;

        CollectionAssert.AreEqual(new[] { sooner, later }, classes.Select(c => c.ClassId).ToArray());
        Assert.IsFalse(classes.Any(c => c.ClassId == notMine), "another instructor's class (D-049)");
        Assert.IsFalse(classes.Any(c => c.ClassId == started), "a class that has started is not upcoming");
        Assert.AreEqual("Spinning", classes[0].ClassName);
        Assert.AreEqual(6, classes[0].Capacity);
        Assert.AreEqual(TimeSpan.FromMinutes(45), classes[0].EndsAt - classes[0].StartsAt);
        Assert.IsEmpty(classes[0].BookedNames);
    }

    [TestMethod]
    public async Task Instructor_sees_the_booked_members_names_but_not_cancelled_ones()
    {
        var (client, me) = await StaffInviteTests.LoggedInStaffWithIdAsync(Factory, StaffRoles.Instructor);
        using var _ = client;
        var classId = await ScheduleAsync(me, Duration.FromDays(3), capacity: 5);
        using var first = Factory.CreateClient();
        using var second = Factory.CreateClient();
        using var cancels = Factory.CreateClient();
        foreach (var member in new[] { first, second, cancels })
        {
            await SignUpConfirmedAndLoggedInAsync(member, SyntheticEmail());
            using var book = await member.PostAsync(BookingUri(classId), null);
            Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode, await book.Content.ReadAsStringAsync());
        }

        using (var cancel = await cancels.DeleteAsync(BookingUri(classId)))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, cancel.StatusCode);
        }

        var entry = (await client.GetFromJsonAsync<MyClass[]>(Url))!.Single(c => c.ClassId == classId);

        // The test members sign up as "Test Member", so each shows as "Test M.".
        CollectionAssert.AreEqual(new[] { "Test M.", "Test M." }, entry.BookedNames);
    }

    [TestMethod]
    public async Task A_class_the_center_cancelled_is_left_out()
    {
        var (client, me) = await StaffInviteTests.LoggedInStaffWithIdAsync(Factory, StaffRoles.Instructor);
        using var _ = client;
        using var manager = await StaffInviteTests.LoggedInStaffAsync(Factory, StaffRoles.Manager);
        var kept = await ScheduleAsync(me, Duration.FromDays(2), capacity: 6);
        var cancelled = await ScheduleAsync(me, Duration.FromDays(3), capacity: 6);
        Assert.IsTrue(
            (await client.GetFromJsonAsync<MyClass[]>(Url))!.Any(c => c.ClassId == cancelled),
            "listed before it is cancelled");

        using (var cancel = await manager.PostAsync(
            new Uri($"/api/booking/staff/classes/{cancelled}/cancel", UriKind.Relative), null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, cancel.StatusCode, await cancel.Content.ReadAsStringAsync());
        }

        var classes = (await client.GetFromJsonAsync<MyClass[]>(Url))!;

        Assert.IsTrue(classes.Any(c => c.ClassId == kept));
        Assert.IsFalse(classes.Any(c => c.ClassId == cancelled), "a cancelled class is not taught (D-068)");
    }

    [TestMethod]
    public async Task Response_holds_only_the_minimum_fields()
    {
        var (client, me) = await StaffInviteTests.LoggedInStaffWithIdAsync(Factory, StaffRoles.Instructor);
        using var _ = client;
        var classId = await ScheduleAsync(me, Duration.FromDays(4), capacity: 3);
        using var member = Factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(member, SyntheticEmail());
        using (var book = await member.PostAsync(BookingUri(classId), null))
        {
            Assert.AreEqual(HttpStatusCode.NoContent, book.StatusCode);
        }

        using var json = JsonDocument.Parse(await client.GetStringAsync(Url));

        var entry = json.RootElement.EnumerateArray().Single(e => e.GetProperty("classId").GetGuid() == classId);
        CollectionAssert.AreEquivalent(
            new[] { "classId", "className", "startsAt", "endsAt", "capacity", "bookedNames" },
            entry.EnumerateObject().Select(property => property.Name).ToArray());
        foreach (var name in entry.GetProperty("bookedNames").EnumerateArray())
        {
            Assert.AreEqual(JsonValueKind.String, name.ValueKind, "a name only, no ID or email");
        }
    }

    [TestMethod]
    [DynamicData(nameof(NonInstructorRoles))]
    public async Task Staff_without_the_instructor_role_are_forbidden(string role)
    {
        using var staff = await StaffInviteTests.LoggedInStaffAsync(Factory, role);

        using var response = await staff.GetAsync(Url);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Member_is_forbidden()
    {
        using var client = Factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(client, SyntheticEmail());

        using var response = await client.GetAsync(Url);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Anonymous_is_unauthorized()
    {
        using var client = Factory.CreateClient();

        using var response = await client.GetAsync(Url);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<Guid> OtherInstructorAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await TestInstructors.CreateAsync(scope.ServiceProvider, "Sam", "Sample");
    }

    private static async Task<Guid> ScheduleAsync(Guid instructor, Duration startsIn, int capacity)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var session = ClassSession.Schedule(
            ClassType.Spinning, instructor, clock.GetCurrentInstant() + startsIn, Duration.FromMinutes(45), capacity);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.Value;
    }

    private static Uri BookingUri(Guid classId) => new($"/api/booking/classes/{classId}/booking", UriKind.Relative);

    private sealed record MyClass(
        Guid ClassId, string ClassName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, string[] BookedNames);
}
