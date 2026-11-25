using System.Net.Http.Json;
using FitnessCenter.Modules.Booking;
using FitnessCenter.Modules.Booking.Domain;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Every class is linked to an instructor's account, and Booking shows the instructor's name
/// through Identity's contract (D-060).
/// </summary>
[TestClass]
public sealed class InstructorTests
{
    [TestMethod]
    [DataRow("Alex", "Example", "Alex E.")]
    [DataRow("  Alex ", " example ", "Alex e.")]
    [DataRow("Alex", "", "Alex")]
    [DataRow("Søren", "Ørsted", "Søren Ø.")]
    [DataRow("Kim", "Élan", "Kim É.")]
    public void Display_name_is_the_first_name_and_the_last_initial(string firstName, string lastName, string expected)
    {
        Assert.AreEqual(expected, DisplayNames.Of(firstName, lastName));
    }

    [TestMethod]
    public async Task Display_names_for_several_accounts_come_back_in_one_call()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var alex = await TestInstructors.CreateAsync(scope.ServiceProvider, "Alex", "Example");
        var sam = await TestInstructors.CreateAsync(scope.ServiceProvider, "Sam", "Sample");
        var kim = await TestInstructors.CreateAsync(scope.ServiceProvider, "Kim", "Placeholder");
        var unknown = Guid.NewGuid();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();

        var names = await accounts.FindDisplayNamesAsync(
            [new(alex), new(sam), new(kim), new(alex), new(unknown)], CancellationToken.None);

        Assert.HasCount(3, names, "one name per known account; duplicates and unknown IDs add none");
        Assert.AreEqual("Alex E.", names[new AccountId(alex)]);
        Assert.AreEqual("Sam S.", names[new AccountId(sam)]);
        Assert.AreEqual("Kim P.", names[new AccountId(kim)]);
        Assert.IsFalse(names.ContainsKey(new AccountId(unknown)));
    }

    [TestMethod]
    public async Task Display_names_of_no_accounts_is_empty()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();

        Assert.IsEmpty(await accounts.FindDisplayNamesAsync([], CancellationToken.None));
    }

    [TestMethod]
    public async Task Schedule_shows_each_class_with_its_own_instructor()
    {
        await using var factory = new ApiFactory();
        Guid[] classes;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var alex = await TestInstructors.CreateAsync(scope.ServiceProvider, "Alex", "Example");
            var sam = await TestInstructors.CreateAsync(scope.ServiceProvider, "Sam", "Sample");
            classes =
            [
                await ScheduleAsync(scope.ServiceProvider, alex, Duration.FromDays(2)),
                await ScheduleAsync(scope.ServiceProvider, sam, Duration.FromDays(3)),
                await ScheduleAsync(scope.ServiceProvider, alex, Duration.FromDays(4)),
            ];
        }

        using var client = factory.CreateClient();
        var schedule = (await client.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule"))!;

        var instructors = classes.Select(id => schedule.Single(entry => entry.ClassId == id).InstructorName).ToArray();
        CollectionAssert.AreEqual(new[] { "Alex E.", "Sam S.", "Alex E." }, instructors);
    }

    [TestMethod]
    public async Task A_renamed_instructor_shows_the_new_name()
    {
        await using var factory = new ApiFactory();
        Guid classId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var instructor = await TestInstructors.CreateAsync(scope.ServiceProvider, "Alex", "Example");
            classId = await ScheduleAsync(scope.ServiceProvider, instructor, Duration.FromDays(2));
            var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
            var account = (await users.FindByIdAsync(instructor.ToString()))!;
            account.FirstName = "Robin";
            Assert.IsTrue((await users.UpdateAsync(account)).Succeeded);
        }

        using var client = factory.CreateClient();
        var schedule = (await client.GetFromJsonAsync<ScheduleEntry[]>("/api/booking/schedule"))!;

        Assert.AreEqual("Robin E.", schedule.Single(entry => entry.ClassId == classId).InstructorName);
    }

    [TestMethod]
    public async Task A_class_needs_an_existing_instructor_account()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();

        await Assert.ThrowsAsync<DbUpdateException>(
            () => ScheduleAsync(scope.ServiceProvider, Guid.NewGuid(), Duration.FromDays(2)));
    }

    [TestMethod]
    public async Task An_instructor_with_classes_cannot_be_deleted()
    {
        await using var factory = new ApiFactory();
        await using var scope = factory.Services.CreateAsyncScope();
        var instructor = await TestInstructors.CreateAsync(scope.ServiceProvider);
        await ScheduleAsync(scope.ServiceProvider, instructor, Duration.FromDays(2));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var account = (await users.FindByIdAsync(instructor.ToString()))!;

        await Assert.ThrowsAsync<DbUpdateException>(() => users.DeleteAsync(account));
    }

    /// <summary>
    /// The development schedule is taught by the development instructor (D-049, D-060) and
    /// still has a small class, so the waitlist can be tried out (D-051).
    /// </summary>
    [TestMethod]
    public async Task Development_schedule_is_taught_by_the_given_instructor()
    {
        var now = Instant.FromUtc(2031, 3, 3, 6, 0); // a Monday, far from other tests' classes
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new FixedClock(now)));
        await using var scope = factory.Services.CreateAsyncScope();
        var instructor = await TestInstructors.CreateAsync(scope.ServiceProvider);

        await factory.Services.SeedDevelopmentScheduleAsync(new AccountId(instructor), CancellationToken.None);

        var sessions = await InWindowAsync(scope.ServiceProvider, now);
        Assert.IsNotEmpty(sessions);
        Assert.IsTrue(sessions.All(s => s.InstructorAccountId == instructor));
        Assert.IsTrue(sessions.Any(s => s.Capacity <= 3), "a small class fills up quickly for the waitlist");
    }

    [TestMethod]
    public async Task Development_schedule_without_an_instructor_adds_no_classes()
    {
        var now = Instant.FromUtc(2032, 3, 1, 6, 0);
        await using var factory = new ApiFactory(services => services.AddSingleton<IClock>(new FixedClock(now)));
        await using var scope = factory.Services.CreateAsyncScope();

        await factory.Services.SeedDevelopmentScheduleAsync(null, CancellationToken.None);

        Assert.IsEmpty(await InWindowAsync(scope.ServiceProvider, now));
    }

    private static async Task<Guid> ScheduleAsync(IServiceProvider services, Guid instructor, Duration startsIn)
    {
        var db = services.GetRequiredService<BookingDbContext>();
        var clock = services.GetRequiredService<IClock>();
        var session = ClassSession.Schedule(
            ClassType.Spinning, instructor, clock.GetCurrentInstant() + startsIn, Duration.FromMinutes(45), 5);
        db.ClassSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.Value;
    }

    private static async Task<List<ClassSession>> InWindowAsync(IServiceProvider services, Instant now)
    {
        var db = services.GetRequiredService<BookingDbContext>();
        var until = now + Duration.FromDays(15);
        return await db.ClassSessions.AsNoTracking().Where(s => s.StartsAt > now && s.StartsAt <= until).ToListAsync();
    }

    private sealed record ScheduleEntry(Guid ClassId, string InstructorName);

    private sealed class FixedClock(Instant now) : IClock
    {
        public Instant GetCurrentInstant() => now;
    }
}
