using FitnessCenter.Modules.Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Booking.Infrastructure;

/// <summary>
/// The Booking module's own DbContext. It owns the <c>booking</c> schema (D-014)
/// and stays internal, so no other module can read or write these tables (rule 3).
/// </summary>
internal sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public const string Schema = "booking";

    public DbSet<ClassType> ClassTypes => Set<ClassType>();

    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ClassType>(type =>
        {
            type.ToTable("class_types");
            type.HasKey(t => t.Id);
            type.Property(t => t.Id).HasConversion(id => id.Value, value => new ClassTypeId(value));
            type.Property(t => t.Name).HasMaxLength(ClassType.NameMaxLength);
            type.HasIndex(t => t.Name).IsUnique();
            type.HasData(ClassType.Seeded.Select(t => new { t.Id, t.Name }));
        });

        modelBuilder.Entity<ClassSession>(session =>
        {
            session.ToTable("class_sessions");
            session.HasKey(s => s.Id);
            session.Property(s => s.Id).HasConversion(id => id.Value, value => new ClassSessionId(value));
            session.Property(s => s.ClassTypeId).HasConversion(id => id.Value, value => new ClassTypeId(value));

            // The FK to identity.AspNetUsers is added by hand in the InstructorAccount migration (D-060).
            session.HasIndex(s => s.InstructorAccountId);
            session.HasOne<ClassType>().WithMany().HasForeignKey(s => s.ClassTypeId);
            session.HasIndex(s => s.StartsAt);
            session.Ignore(s => s.SpotsLeft);
            session.Ignore(s => s.CancelUntil);
            session.Ignore(s => s.IsCancelled);
            session.Ignore(s => s.BookedCount);
            session.Ignore(s => s.WaitlistCount);
            session.Ignore(s => s.Length);
            session.Property(s => s.Status).HasConversion<string>().HasMaxLength(40);

            session.OwnsMany(s => s.Bookings, booking =>
            {
                booking.ToTable("bookings");
                booking.WithOwner().HasForeignKey(b => b.ClassSessionId);
                booking.HasKey(b => b.Id);
                booking.Property(b => b.Id).HasConversion(id => id.Value, value => new ClassBookingId(value));
                booking.Property(b => b.ClassSessionId).HasConversion(id => id.Value, value => new ClassSessionId(value));
                booking.Property(b => b.Status).HasConversion<string>().HasMaxLength(40);
                booking.Ignore(b => b.IsActive);

                // The database's own guard against double booking, next to the rule in ClassSession.
                booking.HasIndex(b => new { b.ClassSessionId, b.AccountId })
                    .IsUnique()
                    .HasFilter("\"Status\" = 'Booked'");
                booking.HasIndex(b => b.AccountId);
            });
            session.Navigation(s => s.Bookings).HasField("_bookings");
            session.Ignore(s => s.Queue);

            session.OwnsMany(s => s.Waitlist, entry =>
            {
                entry.ToTable("waitlist_entries");
                entry.WithOwner().HasForeignKey(e => e.ClassSessionId);
                entry.HasKey(e => e.Id);
                entry.Property(e => e.Id).HasConversion(id => id.Value, value => new WaitlistEntryId(value));
                entry.Property(e => e.ClassSessionId).HasConversion(id => id.Value, value => new ClassSessionId(value));
                entry.Property(e => e.Status).HasConversion<string>().HasMaxLength(40);
                entry.Ignore(e => e.IsWaiting);
                entry.Ignore(e => e.IsOffered);
                entry.Ignore(e => e.IsQueued);

                // The database's own guard against queueing twice, next to the rule in ClassSession.
                // Holding an offer still counts as being on the waitlist (D-067).
                entry.HasIndex(e => new { e.ClassSessionId, e.AccountId })
                    .IsUnique()
                    .HasFilter("\"Status\" IN ('Waiting', 'Offered')");
                entry.HasIndex(e => e.AccountId);
                entry.HasIndex(e => new { e.ClassSessionId, e.Number }).IsUnique();
            });
            session.Navigation(s => s.Waitlist).HasField("_waitlist");
        });
    }
}
