namespace FitnessCenter.Modules.Identity.Contracts;

/// <summary>
/// What other modules may ask the Identity module to do (rule 1).
/// Members uses it during signup (D-034).
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Creates an email/password account. Runs inside the caller's transaction
    /// when there is one, so a failed signup leaves no account behind (D-015).
    /// </summary>
    Task<CreateAccountResult> CreateLocalAccountAsync(NewLocalAccount account, CancellationToken cancellationToken);

    /// <summary>Signs the account in with the auth cookie for the current HTTP request.</summary>
    Task SignInAsync(AccountId accountId, CancellationToken cancellationToken);
}

public readonly record struct AccountId(Guid Value);

/// <summary>The person's name lives on the account, so staff and members share one identity (data-model.md).</summary>
public sealed record NewLocalAccount(string Email, string Password, string FirstName, string LastName);

public sealed record CreateAccountResult(AccountId? AccountId, IReadOnlyList<AccountError> Errors)
{
    public bool Succeeded => AccountId is not null;

    public static CreateAccountResult Success(AccountId id) => new(id, []);

    public static CreateAccountResult Failure(IReadOnlyList<AccountError> errors) => new(null, errors);
}

/// <summary>A reason the account could not be created, e.g. <c>DuplicateEmail</c> or <c>PasswordTooShort</c>.</summary>
public sealed record AccountError(string Code, string Description);
