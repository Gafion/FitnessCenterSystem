using System.Net;
using System.Net.Http.Json;
using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using static FitnessCenter.IntegrationTests.TestAccounts;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Managers and admins create, edit, move and cancel classes and class types (D-068), under the
/// Manager policy (D-062). Booked members are emailed in their language (D-065), checked in
/// Mailpit. The clock is fixed; synthetic accounts only.
/// </summary>
[TestClass]
public sealed class StaffClassTests
{
    // Monday 11 January 2027, 10:00 in Copenhagen (UTC+1).
    private static readonly Instant Now = Instant.FromUtc(2027, 1, 11, 9, 0);

    // Tuesday 12 January 2027, 18:00 in Copenhagen; the cancel deadline is 06:00 that day.
    private static readonly Instant ClassStart = Instant.FromUtc(2027, 1, 12, 17, 0);

    public static IEnumerable<object[]> ManagingRoles => [[StaffRoles.Admin], [StaffRoles.Manager]];

    public static IEnumerable<object[]> OtherRoles => [[StaffRoles.FrontDesk], [StaffRoles.Instructor]];

    [TestMethod]
    [DynamicData(nameof(ManagingRoles))]
    public async Task Admin_and_manager_create_a_class_type_and_schedule_edit_and_list_a_class(string role)
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var staff = await StaffInviteTests.LoggedInStaffAsync(factory, role);
        var instructor = await InstructorAsync(factory);

        var type = await CreateTypeAsync(staff, $"Yoga {Guid.NewGuid():N}");
        var created = await ScheduleAsync(staff, type.Id, instructor, ClassStart, capacity: 8);

        Assert.AreEqual(type.Name, created.ClassName);
        Assert.AreEqual(instructor, created.InstructorAccountId);
        Assert.AreEqual("Ida I.", created.InstructorName);
        Assert.AreEqual(ClassStart.ToDateTimeOffset(), created.StartsAt);
        Assert.AreEqual(45, created.LengthMinutes);
        Assert.AreEqual("Scheduled", created.Status);

        using var edit = await staff.PutAsJsonAsync(
            ClassUri(created.ClassId), Request(type.Id, instructor, ClassStart, capacity: 12, lengthMinutes: 60));
        Assert.AreEqual(HttpStatusCode.OK, edit.StatusCode, await edit.Content.ReadAsStringAsync());
        var edited = (await edit.Content.ReadFromJsonAsync<StaffClass>())!;
        Assert.AreEqual(12, edited.Capacity);
        Assert.AreEqual(60, edited.LengthMinutes);

        var listed = (await staff.GetFromJsonAsync<StaffClass[]>("/api/booking/staff/classes"))!;
        Assert.AreEqual(12, listed.Single(c => c.ClassId == created.ClassId).Capacity);

        var types = (await staff.GetFromJsonAsync<StaffClassType[]>("/api/booking/staff/class-types"))!;
        Assert.IsTrue(types.Any(t => t.Id == type.Id));
        Assert.IsTrue(types.Any(t => t.Name == "Spinning"), "the seeded class types are listed");

        using var schedule = factory.CreateClient();
        var entries = (await schedule.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule"))!;
        Assert.AreEqual(type.Name, entries.Single(e => e.ClassId == created.ClassId).ClassName, "members see the new class");
    }

    [TestMethod]
    [DynamicData(nameof(OtherRoles))]
    public async Task Front_desk_and_instructors_are_forbidden(string role)
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var staff = await StaffInviteTests.LoggedInStaffAsync(factory, role);

        using var list = await staff.GetAsync("/api/booking/staff/classes");
        using var create = await staff.PostAsJsonAsync("/api/booking/staff/class-types", new { name = "Pilates" });

        Assert.AreEqual(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [TestMethod]
    public async Task Members_are_forbidden_and_visitors_must_log_in()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var member = factory.CreateClient();
        await SignUpConfirmedAndLoggedInAsync(member, SyntheticEmail());
        using var visitor = factory.CreateClient();

        using var forbidden = await member.GetAsync("/api/booking/staff/classes");
        using var unauthorized = await visitor.GetAsync("/api/booking/staff/classes");

        Assert.AreEqual(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
    }

    [TestMethod]
    public async Task Cancelling_a_class_cancels_its_bookings_clears_its_waitlist_and_emails_each_member_in_their_language()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var manager = await StaffInviteTests.LoggedInStaffAsync(factory, StaffRoles.Manager);
        var created = await ScheduleAsync(manager, SpinningId, await InstructorAsync(factory), ClassStart, capacity: 2);
        var englishEmail = SyntheticEmail();
        using var english = await MemberAsync(factory, englishEmail);
        var danishEmail = SyntheticEmail();
        using var danish = await MemberAsync(factory, danishEmail, danish: true);
        var waitingEmail = SyntheticEmail();
        using var waiting = await MemberAsync(factory, waitingEmail);
        await SucceedsAsync(english.PostAsync(BookingUri(created.ClassId), null));
        await SucceedsAsync(danish.PostAsync(BookingUri(created.ClassId), null));
        await SucceedsAsync(waiting.PostAsync(WaitlistUri(created.ClassId), null));

        await SucceedsAsync(manager.PostAsync(new Uri($"{ClassUri(created.ClassId)}/cancel", UriKind.Relative), null));

        var text = await TestMailbox.TextToAsync(englishEmail, ClassChangeEmails.CancelledSubject);
        StringAssert.Contains(text, "We have cancelled Spinning, 12 January 2027 at 18:00, so your booking is cancelled too.");
        var tekst = await TestMailbox.TextToAsync(danishEmail, ClassChangeEmails.CancelledSubjectDanish);
        StringAssert.Contains(tekst, "Vi har aflyst Spinning, 12.01.2027 kl. 18.00, så din booking er også aflyst.");
        Assert.AreEqual(0, await TestMailbox.CountToAsync(waitingEmail, ClassChangeEmails.CancelledSubject), "only booked members are emailed");

        Assert.IsEmpty((await english.GetFromJsonAsync<MyBooking[]>("/api/booking/my-bookings"))!);
        var schedule = (await waiting.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule"))!;
        Assert.IsFalse(schedule.Any(e => e.ClassId == created.ClassId), "a cancelled class leaves the schedule");

        var view = (await manager.GetFromJsonAsync<StaffClass>(ClassUri(created.ClassId)))!;
        Assert.AreEqual("Cancelled", view.Status);
        Assert.AreEqual(Now.ToDateTimeOffset(), view.CancelledAt);
        Assert.AreEqual(0, view.BookedCount);
        Assert.AreEqual(0, view.WaitlistCount, "the waitlist is cleared");

        using var book = await waiting.PostAsync(BookingUri(created.ClassId), null);
        Assert.AreEqual(HttpStatusCode.Conflict, book.StatusCode);
        Assert.Contains("class-cancelled", await book.Content.ReadAsStringAsync());
        using var again = await manager.PostAsync(new Uri($"{ClassUri(created.ClassId)}/cancel", UriKind.Relative), null);
        Assert.AreEqual(HttpStatusCode.Conflict, again.StatusCode);
    }

    [TestMethod]
    public async Task Moving_a_class_keeps_its_bookings_emails_the_new_time_and_lets_members_cancel_late()
    {
        var clock = new MovableClock(Now);
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(clock));
        using var manager = await StaffInviteTests.LoggedInStaffAsync(factory, StaffRoles.Manager);
        var instructor = await InstructorAsync(factory);
        var created = await ScheduleAsync(manager, SpinningId, instructor, ClassStart, capacity: 1);
        var bookedEmail = SyntheticEmail();
        using var booked = await MemberAsync(factory, bookedEmail);
        var danishEmail = SyntheticEmail();
        using var waiting = await MemberAsync(factory, danishEmail, danish: true);
        await SucceedsAsync(booked.PostAsync(BookingUri(created.ClassId), null));
        await SucceedsAsync(waiting.PostAsync(WaitlistUri(created.ClassId), null));

        // Wednesday 13 January 2027, 19:30 in Copenhagen.
        var newStart = Instant.FromUtc(2027, 1, 13, 18, 30);
        using var move = await manager.PutAsJsonAsync(ClassUri(created.ClassId), Request(SpinningId, instructor, newStart, capacity: 1));
        Assert.AreEqual(HttpStatusCode.OK, move.StatusCode, await move.Content.ReadAsStringAsync());

        var text = await TestMailbox.TextToAsync(bookedEmail, ClassChangeEmails.MovedSubject);
        StringAssert.Contains(text, "Spinning has moved from 12 January 2027 at 18:00 to 13 January 2027 at 19:30. Your booking is kept.");
        StringAssert.Contains(text, "until the class starts, even after the usual 12-hour deadline");
        Assert.AreEqual(0, await TestMailbox.CountToAsync(danishEmail, ClassChangeEmails.MovedSubjectDanish), "only booked members are emailed");

        var booking = (await booked.GetFromJsonAsync<MyBooking[]>("/api/booking/my-bookings"))!.Single();
        Assert.AreEqual(newStart.ToDateTimeOffset(), booking.StartsAt);
        Assert.AreEqual(newStart.ToDateTimeOffset(), booking.CancelUntil, "after a move the member may cancel until the start");
        var entry = (await waiting.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule"))!.Single(e => e.ClassId == created.ClassId);
        Assert.AreEqual(1, entry.MyWaitlistPlace, "the waitlist stays when a class moves");

        clock.Now = newStart - Duration.FromHours(1);
        await SucceedsAsync(booked.DeleteAsync(BookingUri(created.ClassId)));
    }

    [TestMethod]
    public async Task Capacity_can_not_go_below_the_booked_spots()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var manager = await StaffInviteTests.LoggedInStaffAsync(factory, StaffRoles.Manager);
        var instructor = await InstructorAsync(factory);
        var created = await ScheduleAsync(manager, SpinningId, instructor, ClassStart, capacity: 2);
        using var first = await MemberAsync(factory, SyntheticEmail());
        using var second = await MemberAsync(factory, SyntheticEmail());
        await SucceedsAsync(first.PostAsync(BookingUri(created.ClassId), null));
        await SucceedsAsync(second.PostAsync(BookingUri(created.ClassId), null));

        using var shrink = await manager.PutAsJsonAsync(ClassUri(created.ClassId), Request(SpinningId, instructor, ClassStart, capacity: 1));

        Assert.AreEqual(HttpStatusCode.Conflict, shrink.StatusCode);
        Assert.Contains("capacity-below-bookings", await shrink.Content.ReadAsStringAsync());
        Assert.AreEqual(2, (await manager.GetFromJsonAsync<StaffClass>(ClassUri(created.ClassId)))!.Capacity);
    }

    [TestMethod]
    public async Task More_capacity_offers_the_new_spot_to_the_waitlist()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var manager = await StaffInviteTests.LoggedInStaffAsync(factory, StaffRoles.Manager);
        var instructor = await InstructorAsync(factory);
        var created = await ScheduleAsync(manager, SpinningId, instructor, ClassStart, capacity: 1);
        using var booked = await MemberAsync(factory, SyntheticEmail());
        var waitingEmail = SyntheticEmail();
        using var waiting = await MemberAsync(factory, waitingEmail);
        await SucceedsAsync(booked.PostAsync(BookingUri(created.ClassId), null));
        await SucceedsAsync(waiting.PostAsync(WaitlistUri(created.ClassId), null));

        using var grow = await manager.PutAsJsonAsync(ClassUri(created.ClassId), Request(SpinningId, instructor, ClassStart, capacity: 2));

        Assert.AreEqual(HttpStatusCode.OK, grow.StatusCode, await grow.Content.ReadAsStringAsync());
        Assert.AreEqual(1, await TestMailbox.CountToAsync(waitingEmail, OfferEmails.Subject));
    }

    [TestMethod]
    public async Task A_class_needs_an_instructor_a_known_type_and_a_future_start()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var manager = await StaffInviteTests.LoggedInStaffAsync(factory, StaffRoles.Manager);
        var instructor = await InstructorAsync(factory);
        var notAnInstructor = await AccountWithoutInstructorRoleAsync(factory);

        await RefusedAsync(manager, Request(SpinningId, notAnInstructor, ClassStart, capacity: 5), "instructorAccountId");
        await RefusedAsync(manager, Request(Guid.NewGuid(), instructor, ClassStart, capacity: 5), "classTypeId");
        await RefusedAsync(manager, Request(SpinningId, instructor, Now - Duration.FromHours(1), capacity: 5), "startsAt");
        await RefusedAsync(manager, Request(SpinningId, instructor, ClassStart, capacity: 0), "capacity");
        await RefusedAsync(manager, Request(SpinningId, instructor, ClassStart, capacity: 5, lengthMinutes: 0), "lengthMinutes");
    }

    [TestMethod]
    public async Task Class_type_names_are_unique_ignoring_case_and_can_be_renamed()
    {
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new MovableClock(Now)));
        using var manager = await StaffInviteTests.LoggedInStaffAsync(factory, StaffRoles.Manager);
        var name = $"Pilates {Guid.NewGuid():N}";
        var type = await CreateTypeAsync(manager, name);

        using var duplicate = await manager.PostAsJsonAsync("/api/booking/staff/class-types", new { name = name.ToUpperInvariant() });
        using var empty = await manager.PostAsJsonAsync("/api/booking/staff/class-types", new { name = "  " });
        using var rename = await manager.PutAsJsonAsync($"/api/booking/staff/class-types/{type.Id}", new { name = $"  {name} reformer " });
        using var missing = await manager.PutAsJsonAsync($"/api/booking/staff/class-types/{Guid.NewGuid()}", new { name = "Boxing" });

        Assert.AreEqual(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Contains("class-type-exists", await duplicate.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, rename.StatusCode, await rename.Content.ReadAsStringAsync());
        Assert.AreEqual($"{name} reformer", (await rename.Content.ReadFromJsonAsync<StaffClassType>())!.Name, "the name is trimmed");
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static readonly Guid SpinningId = Guid.Parse("0199a3c0-0000-7000-8000-000000000101");

    private static async Task RefusedAsync(HttpClient manager, object request, string field)
    {
        using var response = await manager.PostAsJsonAsync("/api/booking/staff/classes", request);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, field);
        Assert.Contains($"\"{field}\"", await response.Content.ReadAsStringAsync());
    }

    private static async Task<Guid> InstructorAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await TestInstructors.CreateAsync(scope.ServiceProvider, "Ida", "Instructor");
    }

    /// <summary>A manager account: Manager doesn't make anyone an instructor (D-062).</summary>
    private static async Task<Guid> AccountWithoutInstructorRoleAsync(ApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var email = $"manager-{Guid.NewGuid():N}@example.com";
        var account = new Account { UserName = email, Email = email, EmailConfirmed = true, FirstName = "Max", LastName = "Manager" };
        Assert.IsTrue((await users.CreateAsync(account)).Succeeded);
        Assert.IsTrue((await users.AddToRoleAsync(account, StaffRoles.Manager)).Succeeded);
        return account.Id;
    }

    private static async Task<StaffClassType> CreateTypeAsync(HttpClient staff, string name)
    {
        using var response = await staff.PostAsJsonAsync("/api/booking/staff/class-types", new { name });
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<StaffClassType>())!;
    }

    private static async Task<StaffClass> ScheduleAsync(HttpClient staff, Guid typeId, Guid instructor, Instant startsAt, int capacity)
    {
        using var response = await staff.PostAsJsonAsync("/api/booking/staff/classes", Request(typeId, instructor, startsAt, capacity));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<StaffClass>())!;
    }

    private static object Request(Guid typeId, Guid instructor, Instant startsAt, int capacity, int lengthMinutes = 45) => new
    {
        classTypeId = typeId,
        instructorAccountId = instructor,
        startsAt = startsAt.ToDateTimeOffset(),
        lengthMinutes,
        capacity,
    };

    /// <param name="danish">Sign up without Accept-Language, so the account's language is Danish (D-065).</param>
    private static async Task<HttpClient> MemberAsync(ApiFactory factory, string email, bool danish = false)
    {
        var client = factory.CreateClient();
        if (danish)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
        }

        await SignUpConfirmedAndLoggedInAsync(client, email);
        return client;
    }

    private static async Task SucceedsAsync(Task<HttpResponseMessage> request)
    {
        using var response = await request;
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static string ClassUri(Guid classId) => $"/api/booking/staff/classes/{classId}";

    private static Uri BookingUri(Guid classId) => new($"/api/booking/classes/{classId}/booking", UriKind.Relative);

    private static Uri WaitlistUri(Guid classId) => new($"/api/booking/classes/{classId}/waitlist", UriKind.Relative);

    private sealed record StaffClassType(Guid Id, string Name);

    private sealed record StaffClass(
        Guid ClassId,
        Guid ClassTypeId,
        string ClassName,
        Guid InstructorAccountId,
        string InstructorName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int LengthMinutes,
        int Capacity,
        int BookedCount,
        int WaitlistCount,
        string Status,
        DateTimeOffset? CancelledAt);

    private sealed record ScheduleEntry(Guid ClassId, string ClassName, int? MyWaitlistPlace);

    private sealed record MyBooking(Guid ClassId, DateTimeOffset StartsAt, DateTimeOffset CancelUntil);


    private sealed class MovableClock(Instant now) : IClock
    {
        public Instant Now { get; set; } = now;

        public Instant GetCurrentInstant() => Now;
    }
}
