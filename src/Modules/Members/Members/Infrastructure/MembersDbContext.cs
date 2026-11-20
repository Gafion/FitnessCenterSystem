using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Members.Infrastructure;

/// <summary>
/// The Members module's own DbContext. It owns the <c>members</c> schema (D-014)
/// and stays internal, so no other module can read or write these tables (rule 3).
/// </summary>
internal sealed class MembersDbContext(DbContextOptions<MembersDbContext> options) : DbContext(options)
{
    public const string Schema = "members";

    public DbSet<Member> Members => Set<Member>();

    public DbSet<MembershipType> MembershipTypes => Set<MembershipType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<MembershipType>(type =>
        {
            type.ToTable("membership_types");
            type.HasKey(t => t.Id);
            type.Property(t => t.Id).HasConversion(id => id.Value, value => new MembershipTypeId(value));
            type.Property(t => t.Name).HasMaxLength(100);
            type.Property(t => t.MonthlyPrice)
                .HasColumnName("MonthlyPriceOre")
                .HasConversion(money => money.Ore, ore => new Money(ore));
            type.HasData(new
            {
                MembershipType.General.Id,
                MembershipType.General.Name,
                MembershipType.General.MonthlyPrice,
            });
        });

        modelBuilder.Entity<Member>(member =>
        {
            member.ToTable("members");
            member.HasKey(m => m.Id);
            member.Property(m => m.Id).HasConversion(id => id.Value, value => new MemberId(value));
            member.Property(m => m.MembershipTypeId)
                .HasConversion(id => id.Value, value => new MembershipTypeId(value));
            member.HasIndex(m => m.AccountId).IsUnique();
            member.Property(m => m.Status).HasConversion<string>().HasMaxLength(40);
            member.HasOne<MembershipType>().WithMany().HasForeignKey(m => m.MembershipTypeId);

            member.OwnsMany(m => m.Consents, consent =>
            {
                consent.ToTable("consents");
                consent.WithOwner().HasForeignKey(c => c.MemberId);
                consent.HasKey(c => c.Id);
                consent.Property(c => c.Id).HasConversion(id => id.Value, value => new ConsentId(value));
                consent.Property(c => c.MemberId).HasConversion(id => id.Value, value => new MemberId(value));
                consent.Property(c => c.Kind).HasConversion<string>().HasMaxLength(40);
                consent.Property(c => c.Channel).HasConversion<string>().HasMaxLength(40);
                consent.Property(c => c.TextVersion).HasMaxLength(100);
            });
            member.Navigation(m => m.Consents).HasField("_consents");
        });
    }
}
