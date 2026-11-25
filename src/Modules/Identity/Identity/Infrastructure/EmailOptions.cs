namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// The <c>Email</c> settings section. Locally the AppHost points <c>Email:Smtp</c> at the Mailpit
/// test mailbox (D-038). For real email, the SMTP login comes from user-secrets or the
/// environment, never from git (D-066).
/// </summary>
internal sealed class EmailOptions
{
    public const string SectionName = "Email";

    public SmtpOptions Smtp { get; set; } = new();

    public FromOptions From { get; set; } = new();

    internal sealed class SmtpOptions
    {
        public string? Host { get; set; }

        public int Port { get; set; } = 25;

        public string? Username { get; set; }

        public string? Password { get; set; }

        /// <summary>
        /// Requires STARTTLS. It is always on when a username and password are set, so a login
        /// never goes over plain text. Mailpit speaks plain SMTP, so it is off by default.
        /// </summary>
        public bool UseTls { get; set; }

        public bool HasCredentials => !string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password);
    }

    internal sealed class FromOptions
    {
        /// <summary>The <c>.invalid</c> default can't deliver anywhere; real providers need their own sender (D-066).</summary>
        public string Address { get; set; } = "no-reply@fitnesscenter.invalid";

        public string Name { get; set; } = "FitnessCenter";
    }
}
