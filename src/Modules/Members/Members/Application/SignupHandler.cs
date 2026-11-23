using System.Transactions;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members.Domain;
using FitnessCenter.Modules.Members.Infrastructure;
using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Application;

internal sealed record SignupCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool AcceptTerms,
    string TermsVersion,
    bool StartNow,
    string EarlyStartVersion);

/// <summary>Signup that started with Google: the email and login come from Google (D-037).</summary>
internal sealed record GoogleSignupCommand(
    string FirstName,
    string LastName,
    bool AcceptTerms,
    string TermsVersion,
    bool StartNow,
    string EarlyStartVersion);

internal sealed record SignupResult(MemberId? MemberId, IReadOnlyDictionary<string, string[]> Errors)
{
    public bool Succeeded => MemberId is not null;
}

/// <summary>
/// Signup for the general membership (D-032): creates the Identity account, the member
/// and the consents in one transaction (D-015, D-034), then signs the member in.
/// </summary>
internal sealed class SignupHandler(IAccountService accounts, MembersDbContext db, IClock clock)
{
    public Task<SignupResult> HandleAsync(SignupCommand command, CancellationToken cancellationToken) =>
        SignUpAsync(
            new SignupConsents(command.AcceptTerms, command.TermsVersion, command.StartNow, command.EarlyStartVersion),
            () => accounts.CreateLocalAccountAsync(
                new NewLocalAccount(command.Email, command.Password, command.FirstName, command.LastName),
                cancellationToken),
            cancellationToken);

    public Task<SignupResult> HandleAsync(GoogleSignupCommand command, CancellationToken cancellationToken) =>
        SignUpAsync(
            new SignupConsents(command.AcceptTerms, command.TermsVersion, command.StartNow, command.EarlyStartVersion),
            () => accounts.CreateExternalAccountAsync(
                new NewExternalAccount(command.FirstName, command.LastName), cancellationToken),
            cancellationToken);

    private async Task<SignupResult> SignUpAsync(
        SignupConsents consents, Func<Task<CreateAccountResult>> createAccount, CancellationToken cancellationToken)
    {
        var errors = Validate(consents);
        if (errors.Count > 0)
        {
            return new SignupResult(null, errors);
        }

        Member member;

        // Both modules' DbContexts use the request's shared connection, so the ambient
        // transaction covers the account and the member. If anything below throws,
        // neither is saved.
        using (var transaction = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            TransactionScopeAsyncFlowOption.Enabled))
        {
            var account = await createAccount();
            if (!account.Succeeded)
            {
                return new SignupResult(null, ToFieldErrors(account.Errors));
            }

            member = Member.SignUp(
                account.AccountId!.Value.Value,
                MembershipType.General,
                consents,
                clock.GetCurrentInstant(),
                CenterTime.Zone);

            db.Members.Add(member);
            await db.SaveChangesAsync(cancellationToken);

            transaction.Complete();
        }

        await accounts.SignInAsync(new AccountId(member.AccountId), cancellationToken);
        return new SignupResult(member.Id, new Dictionary<string, string[]>());
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
