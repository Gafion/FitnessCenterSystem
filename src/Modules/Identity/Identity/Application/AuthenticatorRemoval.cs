using FitnessCenter.Modules.Identity.Infrastructure;
using FitnessCenter.SharedKernel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// Turns an account's authenticator app off (D-071): an admin resetting a staff member's, or a
/// member turning their own off. Removes the key and the recovery codes, so neither works again,
/// and emails the account in its own language (D-065).
/// </summary>
internal sealed class AuthenticatorRemoval(
    UserManager<Account> users, IUserStore<Account> store, SmtpEmailSender emails, ILogger<AuthenticatorRemoval> logger)
{
    public enum Reason
    {
        /// <summary>An admin reset a staff member's authenticator. They set it up again at the next login.</summary>
        ResetByAdmin,

        /// <summary>A member turned their own authenticator off.</summary>
        TurnedOffByMember,
    }

    public async Task RemoveAsync(Account account, Reason reason, CancellationToken cancellationToken)
    {
        // Identity has no "remove the recovery codes" on UserManager; an empty list leaves none to redeem.
        if (store is not IUserTwoFactorRecoveryCodeStore<Account> codes)
        {
            throw new InvalidOperationException("The account store doesn't keep recovery codes.");
        }

        await codes.ReplaceCodesAsync(account, [], cancellationToken);
        Check(await users.SetTwoFactorEnabledAsync(account, false));

        // A new random key that is never shown until the next setup, so the old app's codes stop
        // working. It also changes the security stamp, which ends the account's other sessions
        // at their next cookie check.
        Check(await users.ResetAuthenticatorKeyAsync(account));

        try
        {
            var (subject, body) = Email(account.PreferredLanguage, reason, account.FirstName);
            await emails.SendAsync(account.Email!, subject, body, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The change is saved either way, as for the other notification emails (D-067, D-068).
            logger.LogError(exception, "Could not send the authenticator email to account {AccountId}", account.Id);
        }
    }

    public const string ResetSubjectEnglish = "Your authenticator app was reset";

    // UNVERIFIED: the developer's Danish wording, for the owner to check (D-065).
    public const string ResetSubjectDanish = "Din autentificeringsapp er nulstillet";

    public const string TurnedOffSubjectEnglish = "Your authenticator app is turned off";

    // UNVERIFIED: the developer's Danish wording, for the owner to check (D-065).
    public const string TurnedOffSubjectDanish = "Din autentificeringsapp er slået fra";

    internal static (string Subject, string Body) Email(Language language, Reason reason, string firstName) =>
        (language, reason) switch
        {
            (Language.English, Reason.ResetByAdmin) => (ResetSubjectEnglish,
                $"""
                Hi {firstName},

                An administrator reset the authenticator app on your staff account. Your old codes
                and recovery codes no longer work.

                The next time you log in, you'll set up the authenticator app again.

                If you didn't ask for this, tell the center's manager.
                """),
            (Language.English, _) => (TurnedOffSubjectEnglish,
                $"""
                Hi {firstName},

                The authenticator app is now turned off for your account. You log in without a code,
                and your recovery codes no longer work.

                If you didn't do this, log in, turn the authenticator app on again and contact the center.
                """),
            (_, Reason.ResetByAdmin) => (ResetSubjectDanish,
                $"""
                Hej {firstName},

                En administrator har nulstillet autentificeringsappen på din medarbejderkonto. Dine gamle
                koder og gendannelseskoder virker ikke længere.

                Næste gang du logger ind, skal du sætte autentificeringsappen op igen.

                Hvis du ikke har bedt om det, så sig det til centrets leder.
                """),
            _ => (TurnedOffSubjectDanish,
                $"""
                Hej {firstName},

                Autentificeringsappen er nu slået fra for din konto. Du logger ind uden kode, og dine
                gendannelseskoder virker ikke længere.

                Hvis det ikke var dig, så log ind, slå autentificeringsappen til igen og kontakt centret.
                """),
        };

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not turn the authenticator off: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
    }
}
