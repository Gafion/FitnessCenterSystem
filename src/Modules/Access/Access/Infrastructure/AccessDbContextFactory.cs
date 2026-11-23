using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitnessCenter.Modules.Access.Infrastructure;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects.</summary>
internal sealed class AccessDbContextFactory : IDesignTimeDbContextFactory<AccessDbContext>
{
    public AccessDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccessDbContext>()
            .UseNpgsql("Host=design-time-only", npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AccessDbContext.Schema).UseNodaTime())
            .Options;
        return new AccessDbContext(options);
    }
}
