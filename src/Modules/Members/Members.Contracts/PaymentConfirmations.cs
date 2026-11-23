using NodaTime;

namespace FitnessCenter.Modules.Members.Contracts;

/// <summary>
/// A payment confirmation, the event the Payments service will send later (D-020, D-021,
/// architecture.md §13). Until then the payment simulator sends the same event.
/// </summary>
/// <param name="PaymentRef">The sender's id for this payment. A confirmation sent twice is recorded once.</param>
/// <param name="CardLast4">The last four card digits. Never the full card number (D-020).</param>
/// <param name="PeriodFrom">First day the payment covers, in Europe/Copenhagen.</param>
/// <param name="PeriodTo">Last day the payment covers, in Europe/Copenhagen.</param>
public sealed record PaymentConfirmed(
    string PaymentRef,
    Guid MemberId,
    long AmountOre,
    Instant PaidAt,
    string CardLast4,
    LocalDate PeriodFrom,
    LocalDate PeriodTo);

/// <summary>
/// The one way payment confirmations reach the Members module (D-021). The simulator uses it
/// now; the Payments service's event handler will use it later.
/// </summary>
public interface IPaymentConfirmations
{
    Task<PaymentConfirmationResult> ReceiveAsync(PaymentConfirmed confirmation, CancellationToken cancellationToken);
}

public enum PaymentConfirmationResult
{
    Recorded,

    /// <summary>A confirmation with this payment ref was already recorded; nothing changed.</summary>
    AlreadyRecorded,

    UnknownMember,

    /// <summary>
    /// The period does not start the day after the member's paid-until date. Gaps and
    /// overlaps are not decided yet, so nothing is recorded.
    /// </summary>
    PeriodNotContiguous,
}
