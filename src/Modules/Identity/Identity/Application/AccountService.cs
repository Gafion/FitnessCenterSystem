using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Modules.Identity.Application;

internal sealed class AccountService(
    UserManager<Account> users, SignInManager<Account> signIn, IEmailSender<Account> emails, SmtpEmailSender smtp)
    : IAccountService
{
    public async Task<CreateAccountResult> CreateLocalAccountAsync(
        NewLocalAccount newAccount, CancellationToken cancellationToken)
    {
        var errors = ValidateName(newAccount.FirstName, nameof(newAccount.FirstName))
            .Concat(ValidateName(newAccount.LastName, nameof(newAccount.LastName)))
            .ToList();
        if (errors.Count > 0)
        {
            return CreateAccountResult.Failure(errors);
        }

        var account = NewAccount(newAccount.Email, newAccount.FirstName, newAccount.LastName, newAccount.PreferredLanguage);
        return ToResult(account, await users.CreateAsync(account, newAccount.Password));
    }

    public async Task<CreateAccountResult> CreateExternalAccountAsync(
        NewExternalAccount newAccount, CancellationToken cancellationToken)
    {
        var login = await signIn.GetExternalLoginInfoAsync();
        if (login is null)
        {
            return CreateAccountResult.Failure([new AccountError(
                "ExternalLoginMissing", "The Google login has expired. Start again with Google.")]);
        }

        var email = ExternalLogins.Email(login);
        if (email is null || !ExternalLogins.EmailIsVerified(login))
        {
            return CreateAccountResult.Failure([new AccountError(
                "ExternalEmailNotVerified", "Google hasn't verified this email address.")]);
        }

        var errors = ValidateName(newAccount.FirstName, nameof(newAccount.FirstName))
            .Concat(ValidateName(newAccount.LastName, nameof(newAccount.LastName)))
            .ToList();
        if (errors.Count > 0)
        {
            return CreateAccountResult.Failure(errors);
        }

        // No password: this account logs in with Google. Google verified the email.
        var account = NewAccount(email, newAccount.FirstName, newAccount.LastName, newAccount.PreferredLanguage);
        account.EmailConfirmed = true;
        var created = await users.CreateAsync(account);
        if (!created.Succeeded)
        {
            return ToResult(account, created);
        }

        // If this fails, the caller doesn't complete its transaction, so the account goes too.
        return ToResult(account, await users.AddLoginAsync(account, login));
    }

    public async Task SendEmailConfirmationAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var account = await FindAsync(accountId);
        await EmailConfirmation.SendAsync(users, emails, account, signIn.Context.Request);
    }

    public async Task SignInAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var account = await FindAsync(accountId);
        await signIn.SignInAsync(account, isPersistent: true);

        // A pending Google login has done its job once the account is signed in.
        await signIn.Context.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task SendEmailAsync(AccountId accountId, string subject, string body, CancellationToken cancellationToken)
    {
        var account = await FindAsync(accountId);
        await smtp.SendAsync(
            account.Email ?? throw new InvalidOperationException($"Account {accountId.Value} has no email."),
            subject,
            body,
            cancellationToken);
    }

    public async Task<string?> FindFirstNameAsync(AccountId accountId, CancellationToken cancellationToken) =>
        (await users.FindByIdAsync(accountId.Value.ToString()))?.FirstName;

    private async Task<Account> FindAsync(AccountId accountId) =>
        await users.FindByIdAsync(accountId.Value.ToString())
            ?? throw new InvalidOperationException($"Account {accountId.Value} does not exist.");

    public async Task<EmailRecipient?> FindEmailRecipientAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await users.FindByIdAsync(accountId.Value.ToString()) is { } account
            ? new EmailRecipient(account.FirstName, account.PreferredLanguage)
            : null;

    public async Task<IReadOnlyDictionary<AccountId, string>> FindDisplayNamesAsync(
        IReadOnlyCollection<AccountId> accountIds, CancellationToken cancellationToken)
    {
        var ids = accountIds.Select(id => id.Value).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<AccountId, string>();
        }

        var names = await users.Users
            .AsNoTracking()
            .Where(account => ids.Contains(account.Id))
            .Select(account => new { account.Id, account.FirstName, account.LastName })
            .ToListAsync(cancellationToken);
        return names.ToDictionary(
            name => new AccountId(name.Id), name => DisplayNames.Of(name.FirstName, name.LastName));
    }

    public async Task<bool> HasRoleAsync(AccountId accountId, string role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var account = await users.FindByIdAsync(accountId.Value.ToString());
        return account is not null && await users.IsInRoleAsync(account, role);
    }

    private static Account NewAccount(string email, string firstName, string lastName, Language language) => new()
    {
        PreferredLanguage = language,
        Id = Guid.CreateVersion7(),
        UserName = email,
        Email = email,
        FirstName = firstName.Trim(),
        LastName = lastName.Trim(),
    };

    private static CreateAccountResult ToResult(Account account, IdentityResult result) =>
        result.Succeeded
            ? CreateAccountResult.Success(new AccountId(account.Id))
            : CreateAccountResult.Failure(result.Errors
                // The user name is the email, so a taken email is also reported as a taken
                // user name. Callers only know about emails; keep that one.
                .Where(e => e.Code != nameof(IdentityErrorDescriber.DuplicateUserName))
                .Select(e => new AccountError(e.Code, e.Description))
                .ToList());

    private static IEnumerable<AccountError> ValidateName(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return new AccountError($"{field}Required", $"{field} is required.");
        }
        else if (value.Trim().Length > Account.NameMaxLength)
        {
            yield return new AccountError($"{field}TooLong", $"{field} can be at most {Account.NameMaxLength} characters.");
        }
    }
}
