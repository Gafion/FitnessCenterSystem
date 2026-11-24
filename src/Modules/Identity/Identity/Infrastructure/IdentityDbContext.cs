using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// ASP.NET Core Identity's tables in the module's own <c>identity</c> schema (D-014, D-026).
/// Internal, so no other module can read or write these tables (rule 3).
/// </summary>
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<Account, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "identity";

    public DbSet<StaffInvite> StaffInvites => Set<StaffInvite>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<Account>(account =>
        {
            account.Property(a => a.FirstName).HasMaxLength(Account.NameMaxLength);
            account.Property(a => a.LastName).HasMaxLength(Account.NameMaxLength);
        });

        builder.Entity<IdentityRole<Guid>>().HasData(StaffRoleSeed.Roles);

        builder.Entity<StaffInvite>(invite =>
        {
            invite.ToTable("StaffInvites");
            invite.HasKey(i => i.Id);
            invite.Property(i => i.Email).HasMaxLength(256);
            invite.Property(i => i.Role).HasMaxLength(256);
            invite.Property(i => i.CodeHash).HasMaxLength(StaffInvite.CodeHashLength);
            invite.HasOne<Account>().WithMany().HasForeignKey(i => i.InvitedBy).OnDelete(DeleteBehavior.Restrict);
            invite.HasOne<Account>().WithMany().HasForeignKey(i => i.AccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
