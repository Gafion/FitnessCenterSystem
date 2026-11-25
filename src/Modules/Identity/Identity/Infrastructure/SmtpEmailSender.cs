using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// Sends Identity's emails over SMTP. Locally that is the Mailpit test mailbox, which the
/// AppHost starts (D-038); for real email it is Resend's SMTP relay (D-066). Without an SMTP
/// host sending fails instead of silently dropping the email.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender<Account>
{

    public const string ConfirmationSubjectEnglish = "Confirm your email";

    // UNVERIFIED: the developer's Danish wording, for the owner to check (D-065).
    public const string ConfirmationSubjectDanish = "Bekræft din e-mail";

    /// <summary>In the account's language (D-065).</summary>
    public Task SendConfirmationLinkAsync(Account user, string email, string confirmationLink) =>
        user.PreferredLanguage == Language.English
            ? SendAsync(
                email,
                ConfirmationSubjectEnglish,
                $"""
                Hi {user.FirstName},

                Confirm your email to finish signing up:
                {confirmationLink}

                If you didn't sign up, you can ignore this email.
                """)
            : SendAsync(
                email,
                ConfirmationSubjectDanish,
                $"""
                Hej {user.FirstName},

                Bekræft din e-mail for at gøre din tilmelding færdig:
                {confirmationLink}

                Hvis du ikke har tilmeldt dig, kan du se bort fra denne e-mail.
                """);

    // Password reset isn't built yet.
    public Task SendPasswordResetLinkAsync(Account user, string email, string resetLink) =>
        throw new NotSupportedException("Password reset isn't built yet.");

    public Task SendPasswordResetCodeAsync(Account user, string email, string resetCode) =>
        throw new NotSupportedException("Password reset isn't built yet.");

    private Task SendAsync(string to, string subject, string body) => SendAsync(to, subject, body, CancellationToken.None);

    /// <summary>Sends a plain-text email. Other modules' emails come here through <c>IAccountService.SendEmailAsync</c>.</summary>
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var smtp = settings.Smtp;
        if (string.IsNullOrEmpty(smtp.Host))
        {
            throw new InvalidOperationException(
                "No SMTP host is configured (Email:Smtp:Host). Locally, Aspire starts the Mailpit test mailbox.");
        }

        // Checked before connecting, so half a login fails without touching the network.
        var credentials = smtp.HasCredentials ? Credentials(smtp) : null;
        using var message = CreateMessage(settings.From, to, subject, body);

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port, SocketOptions(smtp), cancellationToken);
        if (credentials is not null)
        {
            await client.AuthenticateAsync(credentials, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    internal static MimeMessage CreateMessage(EmailOptions.FromOptions from, string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(from.Name, from.Address));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        return message;
    }

    /// <summary>
    /// Plain SMTP for Mailpit. STARTTLS is required, not just attempted, when TLS is asked for or a
    /// login is set, so a password never goes over an unencrypted connection.
    /// </summary>
    internal static SecureSocketOptions SocketOptions(EmailOptions.SmtpOptions smtp) =>
        smtp.UseTls || smtp.HasCredentials ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

    /// <summary>Both parts or neither. The error names the setting, never its value.</summary>
    internal static System.Net.NetworkCredential Credentials(EmailOptions.SmtpOptions smtp) =>
        string.IsNullOrEmpty(smtp.Username) || string.IsNullOrEmpty(smtp.Password)
            ? throw new InvalidOperationException(
                "Email:Smtp:Username and Email:Smtp:Password must be set together.")
            : new System.Net.NetworkCredential(smtp.Username, smtp.Password);
}
