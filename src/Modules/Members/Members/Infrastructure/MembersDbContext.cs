using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Members.Infrastructure;

/// <summary>
/// The Members module's own DbContext. It owns the <c>members</c> schema (D-014)
/// and stays internal, so no other module can read or write these tables (rule 3).
/// </summary>
internal sealed class MembersDbContext(DbContextOptions<MembersDbContext> options) : DbContext(options)
{
    public const string Schema = "members";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
    }
}
