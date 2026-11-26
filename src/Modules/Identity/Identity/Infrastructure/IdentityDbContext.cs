using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
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

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<Account>(account =>
        {
            account.Property(a => a.FirstName).HasMaxLength(Account.NameMaxLength);
            account.Property(a => a.LastName).HasMaxLength(Account.NameMaxLength);

            // Stored as "da" or "en", the codes the frontend uses (D-065). The migration makes existing accounts Danish.
            account.Property(a => a.PreferredLanguage)
                .HasConversion(language => language.Code(), code => Languages.FromCode(code) ?? Languages.Default)
                .HasMaxLength(2);
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

        // Append-only audit trail (D-072). Restrict keeps an audited account from being deleted with its history.
        builder.Entity<AuditEvent>(audit =>
        {
            audit.ToTable("AuditEvents");
            audit.HasKey(e => e.Id);
            audit.Property(e => e.Action).HasMaxLength(AuditEvent.ActionMaxLength);
            audit.Property(e => e.Detail).HasMaxLength(AuditEvent.DetailMaxLength);
            audit.HasIndex(e => e.OccurredAt);
            audit.HasOne<Account>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
            audit.HasOne<Account>().WithMany().HasForeignKey(e => e.SubjectAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
