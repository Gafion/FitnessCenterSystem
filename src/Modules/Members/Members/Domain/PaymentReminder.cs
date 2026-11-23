using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>
/// The one reminder email for one missing payment (D-042): the member and the paid-until date
/// that passed. A later missing payment has a later paid-until, so it gets its own reminder.
/// The row is saved before the email is sent, so a restart never sends it twice.
/// </summary>
internal sealed class PaymentReminder(PaymentReminderId id, MemberId memberId, LocalDate paidUntil, Instant claimedAt)
{
    public PaymentReminderId Id { get; private set; } = id;

    public MemberId MemberId { get; private set; } = memberId;

    public LocalDate PaidUntil { get; private set; } = paidUntil;

    /// <summary>When a reminder job took this reminder on, just before sending it.</summary>
    public Instant ClaimedAt { get; private set; } = claimedAt;

    /// <summary>
    /// When the email server accepted the email. Null if the job stopped between claiming and
    /// sending: then the email may or may not have gone out, and it is not sent again.
    /// </summary>
    public Instant? SentAt { get; private set; }
}
