using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects.</summary>
internal sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=design-time-only", npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema).UseNodaTime())
            .Options;
        return new IdentityDbContext(options);
    }
}
