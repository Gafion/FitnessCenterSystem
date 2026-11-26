using NodaTime;

namespace FitnessCenter.Modules.Identity.Infrastructure;

/// <summary>
/// One change to an account (D-072): who did it, to whom, what kind of change and when.
/// Append-only: the app never updates a row, and deletes one only when it is a year old (D-072 amendment). Sanitized: account ids, an action name
/// and at most a non-personal detail (a role name or a login method). Never a password, code,
/// key, email, name or other value that was changed.
/// </summary>
internal sealed class AuditEvent
{
    public const int ActionMaxLength = 64;

    public const int DetailMaxLength = 64;

    public Guid Id { get; init; }

    public Instant OccurredAt { get; init; }

    /// <summary>
    /// The account that made the change. The same as the subject when someone changes their own.
    /// Null when the system made it on its own, for example a scheduled job.
    /// </summary>
    public Guid? ActorAccountId { get; init; }

    /// <summary>The account that was changed.</summary>
    public Guid SubjectAccountId { get; init; }

    /// <summary>One of <see cref="AuditActions"/>.</summary>
    public required string Action { get; init; }

    /// <summary>A non-personal detail, such as the role granted or <c>google</c>. Null when there is none.</summary>
    public string? Detail { get; init; }
}

/// <summary>The audited account changes (D-072). Stored as these strings, so renaming one needs a migration.</summary>
internal static class AuditActions
{
    /// <summary>An account was created. Detail: <c>password</c>, <c>google</c> or <c>staff-invite</c>.</summary>
    public const string AccountCreated = "account-created";

    /// <summary>The account's email was confirmed through the emailed link.</summary>
    public const string EmailConfirmed = "email-confirmed";

    /// <summary>The account's email language changed (D-065). The new value isn't logged.</summary>
    public const string LanguageChanged = "language-changed";

    /// <summary>A Google login was linked to an existing account. Detail: <c>google</c>.</summary>
    public const string ExternalLoginLinked = "external-login-linked";

    /// <summary>A staff role was added. Detail: the role. The actor is the admin or manager who sent the invite (D-058).</summary>
    public const string RoleGranted = "role-granted";

    /// <summary>The authenticator app was set up and turned on (D-059).</summary>
    public const string AuthenticatorEnabled = "authenticator-enabled";

    /// <summary>An admin reset a staff member's authenticator app (D-071).</summary>
    public const string AuthenticatorResetByAdmin = "authenticator-reset-by-admin";

    /// <summary>A member turned their own authenticator app off (D-071).</summary>
    public const string AuthenticatorTurnedOff = "authenticator-turned-off";
}
