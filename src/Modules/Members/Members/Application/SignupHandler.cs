using System.Transactions;
using FitnessCenter.Modules.Access.Contracts;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace FitnessCenter.Modules.Members.Application;

internal sealed record SignupCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    LocalDate? DateOfBirth,
    bool AcceptTerms,
    string TermsVersion,
    bool StartNow,
    string EarlyStartVersion);

/// <summary>
/// Signup that started with Google: the email and login come from Google (D-037). The date
/// of birth is asked on the completion page, as in the password signup (D-052, D-054).
/// </summary>
internal sealed record GoogleSignupCommand(
    string FirstName,
    string LastName,
    LocalDate? DateOfBirth,
    bool AcceptTerms,
    string TermsVersion,
    bool StartNow,
    string EarlyStartVersion);

/// <param name="SignedIn">
/// True when the member is logged in now (Google). False when they must confirm their
/// email first (password signup, D-038).
/// </param>
internal sealed record SignupResult(MemberId? MemberId, IReadOnlyDictionary<string, string[]> Errors, bool SignedIn = false)
{
    public bool Succeeded => MemberId is not null;
}

/// <summary>
/// Signup for the general membership (D-032): creates the Identity account, the member,
/// the consents and the member's access card in one transaction (D-003, D-015, D-034),
/// then signs the member in.
/// </summary>
internal sealed class SignupHandler(
    IAccountService accounts, IAccessCards cards, MembersDbContext db, IClock clock, ILogger<SignupHandler> logger)
{
    public Task<SignupResult> HandleAsync(SignupCommand command, CancellationToken cancellationToken) =>
        SignUpAsync(
            new SignupConsents(command.AcceptTerms, command.TermsVersion, command.StartNow, command.EarlyStartVersion),
            command.DateOfBirth,
            () => accounts.CreateLocalAccountAsync(
                new NewLocalAccount(command.Email, command.Password, command.FirstName, command.LastName),
                cancellationToken),
            SendEmailConfirmationAsync,
            cancellationToken);

    public Task<SignupResult> HandleAsync(GoogleSignupCommand command, CancellationToken cancellationToken) =>
        SignUpAsync(
            new SignupConsents(command.AcceptTerms, command.TermsVersion, command.StartNow, command.EarlyStartVersion),
            command.DateOfBirth,
            () => accounts.CreateExternalAccountAsync(
                new NewExternalAccount(command.FirstName, command.LastName), cancellationToken),
            // Google verified the email, so the member is logged in straight away.
            async (accountId, ct) =>
            {
                await accounts.SignInAsync(accountId, ct);
                return true;
            },
            cancellationToken);

    private async Task<SignupResult> SignUpAsync(
        SignupConsents consents,
        LocalDate? dateOfBirth,
        Func<Task<CreateAccountResult>> createAccount,
        Func<AccountId, CancellationToken, Task<bool>> afterCommit,
        CancellationToken cancellationToken)
    {
        var errors = Validate(consents);
        Member member;

        // Both modules' DbContexts use the request's shared connection, so the ambient
        // transaction covers the account and the member. If anything below throws, or we
        // return without completing it, neither is saved.
        using (var transaction = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled))
        {
            // The account is attempted even when the consents are wrong, so the form gets
            // every error in one answer. Returning without Complete() rolls it back.
            var account = await createAccount();
            if (!account.Succeeded)
            {
                foreach (var (field, messages) in ToFieldErrors(account.Errors))
                {
                    errors[field] = messages;
                }
            }

            // Read after the account is created, so a failing clock rolls the account back.
            var now = clock.GetCurrentInstant();
            if (ValidateDateOfBirth(dateOfBirth, now.InZone(CenterTime.Zone).Date) is { } dateOfBirthError)
            {
                errors["dateOfBirth"] = [dateOfBirthError];
            }

            if (errors.Count > 0)
            {
                return new SignupResult(null, errors);
            }

            member = Member.SignUp(
                account.AccountId!.Value.Value,
                MembershipType.General,
                consents,
                now,
                CenterTime.Zone,
                dateOfBirth);

            db.Members.Add(member);
            await db.SaveChangesAsync(cancellationToken);

            // After the member is saved: the card points to it.
            await cards.IssueCardAsync(member.Id.Value, cancellationToken);

            transaction.Complete();
        }

        var signedIn = await afterCommit(new AccountId(member.AccountId), cancellationToken);
        return new SignupResult(member.Id, new Dictionary<string, string[]>(), signedIn);
    }

    /// <summary>
    /// The member is saved by now. If the email can't be sent, signup still succeeds and the
    /// member asks for a new link from the "check your email" page.
    /// </summary>
    private async Task<bool> SendEmailConfirmationAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        try
        {
            await accounts.SendEmailConfirmationAsync(accountId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not send the confirmation email for account {AccountId}", accountId.Value);
        }

        return false;
    }

    private static Dictionary<string, string[]> Validate(SignupConsents consents)
    {
        var errors = new Dictionary<string, string[]>();

        if (!consents.TermsAccepted)
        {
            errors["acceptTerms"] = ["You must accept the terms to sign up."];
        }
        else if (consents.TermsVersion != ConsentTexts.TermsVersion)
        {
            errors["termsVersion"] = ["The terms have changed. Reload the page and read them again."];
        }

        if (consents.EarlyStartRequested && consents.EarlyStartVersion != ConsentTexts.EarlyStartVersion)
        {
            errors["earlyStartVersion"] = ["The early-start text has changed. Reload the page and read it again."];
        }

        return errors;
    }

    /// <summary>
    /// D-052: the date of birth is required and the person must be 18 on the signup date. The
    /// same rule covers password and Google signups (D-054).
    /// </summary>
    private static string? ValidateDateOfBirth(LocalDate? dateOfBirth, LocalDate today) => dateOfBirth switch
    {
        null => "Enter your date of birth.",
        { } born when !MinimumAge.IsMetOn(born, today) => "You must be 18 or older to become a member.",
        _ => null,
    };

    private static Dictionary<string, string[]> ToFieldErrors(IReadOnlyList<AccountError> errors)
    {
        return errors
            .GroupBy(error => FieldFor(error.Code))
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

        static string FieldFor(string code) => code switch
        {
            _ when code.StartsWith("Password", StringComparison.Ordinal) => "password",
            _ when code.Contains("Email", StringComparison.Ordinal) || code.Contains("UserName", StringComparison.Ordinal) => "email",
            _ when code.StartsWith("FirstName", StringComparison.Ordinal) => "firstName",
            _ when code.StartsWith("LastName", StringComparison.Ordinal) => "lastName",
            _ => "",
        };
    }
}
