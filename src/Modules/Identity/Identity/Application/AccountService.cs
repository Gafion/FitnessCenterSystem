using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Application;

internal sealed class AccountService(UserManager<Account> users, SignInManager<Account> signIn) : IAccountService
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

        var account = new Account
        {
            Id = Guid.CreateVersion7(),
            UserName = newAccount.Email,
            Email = newAccount.Email,
            FirstName = newAccount.FirstName.Trim(),
            LastName = newAccount.LastName.Trim(),
        };
        var result = await users.CreateAsync(account, newAccount.Password);

        return result.Succeeded
            ? CreateAccountResult.Success(new AccountId(account.Id))
            : CreateAccountResult.Failure(result.Errors.Select(e => new AccountError(e.Code, e.Description)).ToList());
    }

    public async Task SignInAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        var account = await users.FindByIdAsync(accountId.Value.ToString())
            ?? throw new InvalidOperationException($"Account {accountId.Value} does not exist.");
        await signIn.SignInAsync(account, isPersistent: true);
    }

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
