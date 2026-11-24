using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// Staff accounts come from an admin's invite by email (D-058). The admin picks the email and
/// the role; the invited person follows the link, gives their name and a password, and gets a
/// staff account with that role. No member account is needed.
/// </summary>
internal sealed class StaffInvites(
    IdentityDbContext db, UserManager<Account> users, SmtpEmailSender smtp, IClock clock)
{
    /// <summary>The frontend page that reads the link and calls the accept endpoint.</summary>
    public const string PagePath = "/accept-invite";

    /// <summary>How long a link works. 7 days is a default (D-058, UNVERIFIED as the right length).</summary>
    public static readonly Duration Lifetime = Duration.FromDays(7);

    public async Task<InviteOutcome> InviteAsync(
        Guid adminId, string email, string role, HttpRequest request, CancellationToken cancellationToken)
    {
        email = email.Trim();
        var errors = new Dictionary<string, string[]>();
        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 256)
        {
            errors["email"] = ["Enter a valid email address."];
        }

        if (!StaffRoles.All.Contains(role))
        {
            errors["role"] = [$"Role must be one of: {string.Join(", ", StaffRoles.All)}."];
        }

        if (errors.Count > 0)
        {
            return InviteOutcome.Invalid(errors);
        }

        // One account per email (RequireUniqueEmail). A member who should also be staff
        // can't get a second account for now; that is an open point in D-058.
        if (await users.FindByEmailAsync(email) is not null)
        {
            return InviteOutcome.AccountExists;
        }

        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = clock.GetCurrentInstant();
        var invite = new StaffInvite
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            Role = role,
            CodeHash = Hash(code),
            InvitedBy = adminId,
            CreatedAt = now,
            ExpiresAt = now + Lifetime,
        };
        db.StaffInvites.Add(invite);
        await db.SaveChangesAsync(cancellationToken);

        // The browser's address, as for email confirmation links (EmailConfirmation).
        var link = $"{request.Scheme}://{request.Host}{PagePath}"
            + QueryString.Create(new Dictionary<string, string?> { ["invite"] = invite.Id.ToString(), ["code"] = code });
        await smtp.SendAsync(
            email,
            "You're invited to join the staff",
            $"""
            Hi,

            You're invited to a staff account with the role {role}.
            Follow this link to set your name and password:
            {link}

            The link works for {Lifetime.Days} days and only once.
            If you weren't expecting this, you can ignore this email.
            """,
            cancellationToken);

        return InviteOutcome.Sent(invite);
    }

    public async Task<AcceptOutcome> AcceptAsync(
        Guid inviteId, string code, string firstName, string lastName, string password,
        CancellationToken cancellationToken)
    {
        using var transaction = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled);

        // Locked, so two requests with the same link take turns and only one creates an account.
        var invite = await db.StaffInvites
            .FromSql($"""SELECT * FROM identity."StaffInvites" WHERE "Id" = {inviteId} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);
        if (invite is null || !CodeMatches(invite, code))
        {
            return AcceptOutcome.InvalidLink;
        }

        if (invite.AcceptedAt is not null)
        {
            return AcceptOutcome.Used;
        }

        var now = clock.GetCurrentInstant();
        if (now >= invite.ExpiresAt)
        {
            return AcceptOutcome.Expired;
        }

        var errors = ValidateName(firstName, "firstName").Concat(ValidateName(lastName, "lastName"))
            .ToDictionary(e => e.Field, e => new[] { e.Message });
        if (errors.Count > 0)
        {
            return AcceptOutcome.Invalid(errors);
        }

        if (await users.FindByEmailAsync(invite.Email) is not null)
        {
            return AcceptOutcome.AccountExists;
        }

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = invite.Email,
            Email = invite.Email,
            // Following the emailed link proves the address (D-038).
            EmailConfirmed = true,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
        };
        var created = await users.CreateAsync(account, password);
        if (!created.Succeeded)
        {
            return AcceptOutcome.Invalid(new Dictionary<string, string[]>
            {
                ["password"] = created.Errors.Select(e => e.Description).ToArray(),
            });
        }

        var added = await users.AddToRoleAsync(account, invite.Role);
        if (!added.Succeeded)
        {
            throw new InvalidOperationException(
                $"Adding role {invite.Role} failed: {string.Join(", ", added.Errors.Select(e => e.Code))}");
        }

        invite.AcceptedAt = now;
        invite.AccountId = account.Id;
        await db.SaveChangesAsync(cancellationToken);

        transaction.Complete();
        return AcceptOutcome.Accepted;
    }

    private static string Hash(string code) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private static bool CodeMatches(StaffInvite invite, string code) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(code)), Encoding.ASCII.GetBytes(invite.CodeHash));

    private static IEnumerable<(string Field, string Message)> ValidateName(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return (field, "This field is required.");
        }
        else if (value.Trim().Length > Account.NameMaxLength)
        {
            yield return (field, $"At most {Account.NameMaxLength} characters.");
        }
    }
}

internal sealed record InviteOutcome(StaffInvite? Invite, IReadOnlyDictionary<string, string[]>? Errors, bool AccountAlreadyExists)
{
    public static readonly InviteOutcome AccountExists = new(null, null, true);

    public static InviteOutcome Sent(StaffInvite invite) => new(invite, null, false);

    public static InviteOutcome Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors, false);
}

internal enum AcceptStatus
{
    Accepted,
    InvalidLink,
    Used,
    Expired,
    AccountExists,
    Invalid,
}

internal sealed record AcceptOutcome(AcceptStatus Status, IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static readonly AcceptOutcome Accepted = new(AcceptStatus.Accepted);
    public static readonly AcceptOutcome InvalidLink = new(AcceptStatus.InvalidLink);
    public static readonly AcceptOutcome Used = new(AcceptStatus.Used);
    public static readonly AcceptOutcome Expired = new(AcceptStatus.Expired);
    public static readonly AcceptOutcome AccountExists = new(AcceptStatus.AccountExists);

    public static AcceptOutcome Invalid(IReadOnlyDictionary<string, string[]> errors) => new(AcceptStatus.Invalid, errors);
}
