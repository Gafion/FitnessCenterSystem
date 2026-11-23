using FitnessCenter.Modules.Booking.Application;
using FitnessCenter.Modules.Booking.Endpoints;
using FitnessCenter.Modules.Booking.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddHealthChecks().AddDbContextCheck<BookingDbContext>("booking-db");

        return services;
    }

    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapClassEndpoints();
    }

    /// <summary>Applies this module's migrations. The host calls it at startup in Development.</summary>
    public static async Task MigrateBookingDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>Fills the next 14 days with synthetic classes. Development only (D-022).</summary>
    public static async Task SeedDevelopmentScheduleAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await DevelopmentSchedule.EnsureAsync(
            scope.ServiceProvider.GetRequiredService<BookingDbContext>(),
            scope.ServiceProvider.GetRequiredService<IClock>(),
            cancellationToken);
    }
}
