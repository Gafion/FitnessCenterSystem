using FitnessCenter.Modules.Access.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Access.Infrastructure;

/// <summary>
/// The Access module's own DbContext. It owns the <c>access</c> schema (D-014)
/// and stays internal, so no other module can read or write these tables (rule 3).
/// </summary>
internal sealed class AccessDbContext(DbContextOptions<AccessDbContext> options) : DbContext(options)
{
    public const string Schema = "access";

    public DbSet<AccessCard> AccessCards => Set<AccessCard>();

    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AccessCard>(card =>
        {
            card.ToTable("access_cards");
            card.HasKey(c => c.Id);
            card.Property(c => c.Id).HasConversion(id => id.Value, value => new AccessCardId(value));
            card.Property(c => c.Uid).HasMaxLength(CardUid.MaxLength);
            card.HasIndex(c => c.Uid).IsUnique();
            // One card per member for now; more cards per member is an open question.
            card.HasIndex(c => c.MemberId).IsUnique();
        });

        modelBuilder.Entity<Attendance>(attendance =>
        {
            attendance.ToTable("attendances");
            attendance.HasKey(a => a.Id);
            attendance.Property(a => a.Id).HasConversion(id => id.Value, value => new AttendanceId(value));
            attendance.Property(a => a.AccessCardId).HasConversion(id => id.Value, value => new AccessCardId(value));
            attendance.HasOne<AccessCard>().WithMany().HasForeignKey(a => a.AccessCardId).OnDelete(DeleteBehavior.Restrict);
            attendance.HasIndex(a => new { a.MemberId, a.SwipedAt });
        });
    }
}
