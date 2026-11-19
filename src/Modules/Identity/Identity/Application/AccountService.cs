using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace FitnessCenter.Modules.Identity.Application;

internal sealed class AccountService(UserManager<Account> users, SignInManager<Account> signIn) : IAccountService
{
    public async Task<CreateAccountResult> CreateLocalAccountAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var account = new Account { Id = Guid.CreateVersion7(), UserName = email, Email = email };
        var result = await users.CreateAsync(account, password);

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
}
