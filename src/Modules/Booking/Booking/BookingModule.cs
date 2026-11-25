using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Contracts;
using FitnessCenter.Modules.Booking.Endpoints;
using FitnessCenter.Modules.Booking.Infrastructure;
using FitnessCenter.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Npgsql;

namespace FitnessCenter.Modules.Booking;

/// <summary>
/// Entry point of the Booking module: class schedule, booking and cancelling (D-022).
/// The API host calls these methods and nothing else in this project.
/// </summary>
public static class BookingModule
{
    public static IServiceCollection AddBookingModule(this IServiceCollection services)
    {
        // The shared, request-scoped connection lets one transaction span modules (D-015).
        services.AddDbContext<BookingDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlConnection>(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BookingDbContext.Schema).UseNodaTime()));

        services.AddScoped<BookingHandler>();
        services.AddScoped<OfferEmails>();
        services.AddScoped<StaffClassHandler>();
        services.AddScoped<ClassChangeEmails>();
        services.AddHostedService<WaitlistOfferJob>();
        services.AddScoped<IMemberBookings, MemberBookings>();

        services.AddHealthChecks().AddDbContextCheck<BookingDbContext>("booking-db");

        return services;
    }

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapClassEndpoints().MapStaffClassEndpoints();
    }

    /// <summary>Applies this module's migrations. The host calls it at startup in Development.</summary>
    public static async Task MigrateBookingDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Fills the next 14 days with synthetic classes taught by <paramref name="instructor"/>.
    /// Development only (D-022). Every class needs an instructor account (D-060), so without the
    /// development instructor (D-049) no classes are seeded.
    /// </summary>
    public static async Task SeedDevelopmentScheduleAsync(
        this IServiceProvider services, AccountId? instructor, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        if (instructor is not { } account)
        {
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(DevelopmentSchedule))
                .LogInformation(
                    "No development classes seeded: every class needs an instructor account (D-060). "
                    + "Set Identity:DevelopmentStaffPassword with dotnet user-secrets to seed the development staff.");
            return;
        }

        await DevelopmentSchedule.EnsureAsync(
            scope.ServiceProvider.GetRequiredService<BookingDbContext>(),
            scope.ServiceProvider.GetRequiredService<IClock>(),
            account.Value,
            cancellationToken);
    }
}
