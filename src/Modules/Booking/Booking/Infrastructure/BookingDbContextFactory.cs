using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitnessCenter.Modules.Booking.Infrastructure;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects.</summary>
internal sealed class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql("Host=design-time-only", npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BookingDbContext.Schema).UseNodaTime())
            .Options;
        return new BookingDbContext(options);
    }
}
