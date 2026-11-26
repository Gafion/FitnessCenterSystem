using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Every module records its actions in the audit trail (D-072): bookings, the waitlist, staff
/// class changes, memberships, payments and check-ins. Synthetic accounts only.
/// </summary>
[TestClass]
public sealed class AuditCoverageTests
{
    private static ApiFactory? _factory;

    private static ApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

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
    public async Task Booking_and_cancelling_a_class_are_audited_with_the_class_id()
    {
        var (client, id) = await MemberAsync();
        using var _ = client;
        var classId = await ScheduleClassAsync(capacity: 5);

        await SucceedsAsync(client.PostAsync(Uri($"/api/booking/classes/{classId}/booking"), null));
        await SucceedsAsync(client.DeleteAsync(Uri($"/api/booking/classes/{classId}/booking")));

        var events = await EventsAboutAsync(id);
        var booked = events.Single(e => e.Action == "class-booked");
        Assert.AreEqual(id, booked.ActorAccountId);
        Assert.AreEqual(classId.ToString(), booked.Detail);
        Assert.AreEqual(classId.ToString(), events.Single(e => e.Action == "booking-cancelled").Detail);
    }

    [TestMethod]
    public async Task Staff_cancelling_a_class_is_audited_for_the_staff_member_and_each_booked_member()
    {
        var (member, memberId) = await MemberAsync();
        using var _ = member;
        var classId = await ScheduleClassAsync(capacity: 5);
        await SucceedsAsync(member.PostAsync(Uri($"/api/booking/classes/{classId}/booking"), null));
        var (manager, managerId) = await StaffInviteTests.LoggedInStaffWithIdAsync(Factory, StaffRoles.Manager);
        using var managerClient = manager;

        await SucceedsAsync(manager.PostAsync(Uri($"/api/booking/staff/classes/{classId}/cancel"), null));

        var cancelled = (await EventsAboutAsync(managerId)).Single(e => e.Action == "class-cancelled");
        Assert.AreEqual(managerId, cancelled.ActorAccountId);
        Assert.AreEqual(classId.ToString(), cancelled.Detail);
        var booking = (await EventsAboutAsync(memberId)).Single(e => e.Action == "booking-cancelled-by-staff");
        Assert.AreEqual(managerId, booking.ActorAccountId, "the manager cancelled the member's booking");
    }

    [TestMethod]
    public async Task Membership_signup_payment_cancellation_and_undo_are_audited()
    {
        var (client, id) = await MemberAsync();
        using var _ = client;
        await TestAccounts.PayAsync(client);

        await SucceedsAsync(client.PostAsync(Uri("/api/members/me/cancellation"), null));
        await SucceedsAsync(client.DeleteAsync(Uri("/api/members/me/cancellation")));

        var events = await EventsAboutAsync(id);
        var actions = events.Select(e => e.Action).ToList();
        CollectionAssert.IsSubsetOf(
            new[] { "membership-started", "payment-recorded", "membership-cancelled", "membership-cancellation-undone" },
            actions);
        Assert.IsNull(events.Single(e => e.Action == "payment-recorded").ActorAccountId, "the payment service, not a person");
        Assert.IsNull(events.Single(e => e.Action == "payment-recorded").Detail, "no amount is logged");
    }

    [TestMethod]
    public async Task Check_ins_and_refused_check_ins_are_audited()
    {
        var (unpaid, unpaidId) = await MemberAsync();
        using var _ = unpaid;
        var (paid, paidId) = await MemberAsync();
        using var paidClient = paid;
        await TestAccounts.PayAsync(paid);

        await SucceedsAsync(unpaid.PostAsync(Uri("/api/access/simulation/reader/swipes/my-card"), null));
        await SucceedsAsync(paid.PostAsync(Uri("/api/access/simulation/reader/swipes/my-card"), null));

        var refused = (await EventsAboutAsync(unpaidId)).Single(e => e.Action == "check-in-refused");
        Assert.AreEqual("payment-missing", refused.Detail);
        Assert.IsTrue((await EventsAboutAsync(paidId)).Any(e => e.Action == "checked-in"));
    }

    private static async Task<(HttpClient Client, Guid Id)> MemberAsync()
    {
        var email = TestAccounts.SyntheticEmail();
        var client = Factory.CreateClient();
        await TestAccounts.SignUpConfirmedAndLoggedInAsync(client, email);
        return (client, await AuditTrailTests.AccountIdAsync(Factory, email));
    }

    private static async Task<Guid> ScheduleClassAsync(int capacity)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var session = ClassSession.Schedule(
            ClassType.Spinning, await TestInstructors.CreateAsync(scope.ServiceProvider),
            clock.GetCurrentInstant() + Duration.FromDays(2), Duration.FromMinutes(45), capacity);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.Value;
    }

    private static async Task<IReadOnlyList<AuditEvent>> EventsAboutAsync(Guid accountId) =>
        (await AuditTrailTests.AuditAsync(Factory)).Where(e => e.SubjectAccountId == accountId).ToList();

    private static async Task SucceedsAsync(Task<HttpResponseMessage> request)
    {
        using var response = await request;
        Assert.IsTrue(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    private static Uri Uri(string path) => new(path, UriKind.Relative);
}
