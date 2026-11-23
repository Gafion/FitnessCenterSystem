using FitnessCenter.SharedKernel;
using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

/// <summary>The days a payment covers, both inclusive, as Copenhagen dates (D-020).</summary>
internal readonly record struct PaymentPeriod
{
    public PaymentPeriod(LocalDate from, LocalDate to)
    {
        if (to < from)
        {
            throw new ArgumentException($"The period ends ({to}) before it starts ({from}).", nameof(to));
        }

        From = from;
        To = to;
    }

    public LocalDate From { get; }

    public LocalDate To { get; }

    /// <summary>One calendar month from <paramref name="from"/>: 1 March to 31 March, 15 March to 14 April.</summary>
    public static PaymentPeriod MonthFrom(LocalDate from) => new(from, from.PlusMonths(1).PlusDays(-1));
}

/// <summary>
/// The core's copy of one payment confirmation (D-020, data-model.md <c>PAYMENT_RECORD</c>).
/// Never edited. Holds the last four card digits only, never the full card number.
/// </summary>
internal sealed class PaymentRecord
{
    public PaymentRecord(
        PaymentRecordId id, MemberId memberId, string paymentRef, Money amount, Instant paidAt, string cardLast4, PaymentPeriod period)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentRef);
        if (cardLast4.Length != 4 || !cardLast4.All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Only the last four card digits are stored.", nameof(cardLast4));
        }

        Id = id;
        MemberId = memberId;
        PaymentRef = paymentRef;
        Amount = amount;
        PaidAt = paidAt;
        CardLast4 = cardLast4;
        PeriodFrom = period.From;
        PeriodTo = period.To;
    }

    // For EF Core.
    private PaymentRecord()
    {
        PaymentRef = null!;
        CardLast4 = null!;
    }

    public PaymentRecordId Id { get; private set; }

    public MemberId MemberId { get; private set; }

    /// <summary>The sender's id for the payment; unique, so a repeated confirmation is recorded once.</summary>
    public string PaymentRef { get; private set; }

    public Money Amount { get; private set; }

    public Instant PaidAt { get; private set; }

    public string CardLast4 { get; private set; }

    public LocalDate PeriodFrom { get; private set; }

    public LocalDate PeriodTo { get; private set; }

    public PaymentPeriod Period => new(PeriodFrom, PeriodTo);
}
