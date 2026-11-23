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
            type.HasData(ClassType.All.Select(t => new { t.Id, t.Name }));
        });

        modelBuilder.Entity<ClassSession>(session =>
        {
            session.ToTable("class_sessions");
            session.HasKey(s => s.Id);
            session.Property(s => s.Id).HasConversion(id => id.Value, value => new ClassSessionId(value));
            session.Property(s => s.ClassTypeId).HasConversion(id => id.Value, value => new ClassTypeId(value));
            session.Property(s => s.InstructorName).HasMaxLength(ClassSession.InstructorNameMaxLength);
            session.HasOne<ClassType>().WithMany().HasForeignKey(s => s.ClassTypeId);
            session.HasIndex(s => s.StartsAt);
            session.Ignore(s => s.SpotsLeft);
            session.Ignore(s => s.CancelUntil);

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
        });
    }
}
