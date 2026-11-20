using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FitnessCenter.Modules.Members.Infrastructure;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without starting the API. It never connects.</summary>
internal sealed class MembersDbContextFactory : IDesignTimeDbContextFactory<MembersDbContext>
{
    public MembersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MembersDbContext>()
            .UseNpgsql("Host=design-time-only", npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MembersDbContext.Schema).UseNodaTime())
            .Options;
        return new MembersDbContext(options);
    }
}
