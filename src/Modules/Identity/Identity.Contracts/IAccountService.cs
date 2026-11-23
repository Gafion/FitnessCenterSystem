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

    /// <summary>
    /// Creates an account for the Google login waiting in the current HTTP request (D-037).
    /// The email comes from Google, not the caller. Runs inside the caller's transaction like
    /// <see cref="CreateLocalAccountAsync"/>.
    /// </summary>
    Task<CreateAccountResult> CreateExternalAccountAsync(NewExternalAccount account, CancellationToken cancellationToken);

    /// <summary>
    /// Emails the account a link to confirm its address. The account can't log in until it
    /// does (D-038). Call it after the caller's transaction has committed.
    /// </summary>
    Task SendEmailConfirmationAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>Signs the account in with the auth cookie for the current HTTP request.</summary>
    Task SignInAsync(AccountId accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Emails the account's address. The caller writes the text; Identity knows the address.
    /// Members uses it for the payment reminder (D-042). Needs no HTTP request, so a background
    /// job may call it. Throws when the email can't be sent.
    /// </summary>
    Task SendEmailAsync(AccountId accountId, string subject, string body, CancellationToken cancellationToken);

    /// <returns>The account's first name, or null when there is no such account. Members uses it at check-in.</returns>
    Task<string?> FindFirstNameAsync(AccountId accountId, CancellationToken cancellationToken);
}

public readonly record struct AccountId(Guid Value);

/// <summary>The person's name lives on the account, so staff and members share one identity (data-model.md).</summary>
public sealed record NewLocalAccount(string Email, string Password, string FirstName, string LastName);

/// <summary>The names the person confirmed; Google may have suggested them.</summary>
public sealed record NewExternalAccount(string FirstName, string LastName);

public sealed record CreateAccountResult(AccountId? AccountId, IReadOnlyList<AccountError> Errors)
{
    public bool Succeeded => AccountId is not null;

    public static CreateAccountResult Success(AccountId id) => new(id, []);

    public static CreateAccountResult Failure(IReadOnlyList<AccountError> errors) => new(null, errors);
}

/// <summary>
/// A reason the account could not be created, e.g. <c>DuplicateEmail</c>, <c>PasswordTooShort</c>
/// or <c>ExternalLoginMissing</c>.
/// </summary>
public sealed record AccountError(string Code, string Description);
