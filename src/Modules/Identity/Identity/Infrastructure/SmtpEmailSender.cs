using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.SharedKernel;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// Sends Identity's emails over SMTP. Locally that is the Mailpit test mailbox, which the
/// AppHost starts (D-038). The real email provider is still open, so without an SMTP host
/// sending fails instead of silently dropping the email.
/// </summary>
internal sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender<Account>
{
    // Placeholder sender until the center's domain and provider are chosen (D-038).
    private static readonly MailboxAddress From = new("FitnessCenter", "no-reply@fitnesscenter.invalid");

    public const string ConfirmationSubjectEnglish = "Confirm your email";

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
        var host = configuration["Email:Smtp:Host"];
        if (string.IsNullOrEmpty(host))
        {
            throw new InvalidOperationException(
                "No SMTP host is configured (Email:Smtp:Host). Locally, Aspire starts the Mailpit test mailbox.");
        }

        var port = configuration.GetValue("Email:Smtp:Port", 25);

        using var message = new MimeMessage();
        message.From.Add(From);
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        // Mailpit speaks plain SMTP on localhost. A real provider will need TLS and a login.
        await client.ConnectAsync(host, port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
