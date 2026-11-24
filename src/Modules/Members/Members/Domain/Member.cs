using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

internal enum MemberStatus
{
    /// <summary>
    /// Signed up; no payment confirmation yet (D-032). Check-in is refused until the first
    /// payment (D-046); see <see cref="PaymentStanding.NeverPaid"/>.
    /// </summary>
    AwaitingFirstPayment,

    /// <summary>
    /// At least one payment confirmation arrived (D-020). Whether a payment is missing follows
    /// from <see cref="Member.PaidUntil"/>, not from this status.
    /// </summary>
    Active,
}

internal enum PaymentOutcome
{
    Applied,

    /// <summary>The period doesn't start the day after paid-until; gaps and overlaps are undecided.</summary>
    NotContiguous,
}

internal enum CancellationOutcome
{
    Cancelled,

    /// <summary>The membership was already cancelled; the first cancellation stands (FIT-70).</summary>
    AlreadyCancelled,
}

internal enum UndoCancellationOutcome
{
    Undone,

    /// <summary>There is no cancellation to undo; nothing changes (FIT-95).</summary>
    NothingPending,

    /// <summary>The effective date has passed, so the membership has ended (D-055).</summary>
    AlreadyEffective,
}

/// <summary>
/// A person's membership at the center. Name and email live on the Identity account
/// (data-model.md); the member links to it by account id.
/// </summary>
internal sealed class Member
{
    private readonly List<Consent> _consents = [];
    private readonly List<CancellationRecord> _cancellations = [];

    private Member(MemberId id, Guid accountId, MembershipTypeId membershipTypeId, LocalDate memberSince)
    {
        Id = id;
        AccountId = accountId;
        MembershipTypeId = membershipTypeId;
        MemberSince = memberSince;
        Status = MemberStatus.AwaitingFirstPayment;
    }

    public MemberId Id { get; private set; }

    /// <summary>The Identity account this member signs in with (identity schema; FK only, rule 3).</summary>
    public Guid AccountId { get; private set; }

    public MembershipTypeId MembershipTypeId { get; private set; }

    /// <summary>The signup date in Europe/Copenhagen, where the center's business rules apply.</summary>
    public LocalDate MemberSince { get; private set; }

    /// <summary>
    /// Collected at signup to check the 18+ rule (D-052). Null for members who signed up
    /// before D-052 and for Google signups, whose date of birth is undecided (FIT-68).
    /// </summary>
    public LocalDate? DateOfBirth { get; private set; }

    public MemberStatus Status { get; private set; }

    /// <summary>Last day covered by a payment confirmation (D-020). Null until the first one.</summary>
    public LocalDate? PaidUntil { get; private set; }

    /// <summary>When the member cancelled (UTC). Null while the membership is not cancelled (D-048).</summary>
    public Instant? CancelledAt { get; private set; }

    /// <summary>
    /// The last day of access (Copenhagen); the cancellation takes effect at its end (D-048).
    /// Null while the membership is not cancelled.
    /// </summary>
    public LocalDate? CancellationEffectiveOn { get; private set; }

    public IReadOnlyList<Consent> Consents => _consents;

    /// <summary>
    /// Every cancellation the member made, including undone ones (FIT-95). Undoing clears
    /// <see cref="CancelledAt"/> and <see cref="CancellationEffectiveOn"/>, but the record stays.
    /// </summary>
    public IReadOnlyList<CancellationRecord> Cancellations => _cancellations;

    /// <summary>
    /// Signs a person up. The terms must be accepted; starting early is optional.
    /// <paramref name="now"/> comes from the injected clock, never the system clock (rule 2).
    /// A given <paramref name="dateOfBirth"/> must make the person 18 or older on the signup
    /// date in <paramref name="businessZone"/> (D-052).
    /// </summary>
    public static Member SignUp(
        Guid accountId,
        MembershipType membershipType,
        SignupConsents consents,
        Instant now,
        DateTimeZone businessZone,
        LocalDate? dateOfBirth = null)
    {
        if (!consents.TermsAccepted)
        {
            throw new InvalidOperationException("The terms must be accepted to sign up.");
        }

        var today = now.InZone(businessZone).Date;
        if (dateOfBirth is { } born && !MinimumAge.IsMetOn(born, today))
        {
            throw new InvalidOperationException("Members must be 18 or older.");
        }

        var member = new Member(MemberId.New(), accountId, membershipType.Id, today) { DateOfBirth = dateOfBirth };

        member._consents.Add(new Consent(
            ConsentId.New(), member.Id, ConsentKind.Terms, consents.TermsVersion, now, ConsentChannel.Online));

        if (consents.EarlyStartRequested)
        {
            member._consents.Add(new Consent(
                ConsentId.New(), member.Id, ConsentKind.EarlyStart, consents.EarlyStartVersion, now, ConsentChannel.Online));
        }

        return member;
    }

    /// <summary>
    /// Where the next monthly payment period starts: the day after paid-until, or
    /// <paramref name="today"/> (Copenhagen) before the first payment.
    /// </summary>
    public LocalDate NextPeriodStart(LocalDate today) => PaidUntil?.PlusDays(1) ?? today;

    /// <summary>Whether a payment is missing at <paramref name="now"/> (D-020, D-042).</summary>
    public PaymentStanding PaymentStandingAt(Instant now, DateTimeZone businessZone) =>
        MissingPayment.StandingAt(PaidUntil, now, businessZone);

    /// <summary>
    /// Whether the member has access on <paramref name="day"/> (Copenhagen). A cancelled
    /// membership gives access up to and including <see cref="CancellationEffectiveOn"/>, and
    /// not after (D-048).
    /// </summary>
    public bool HasAccessOn(LocalDate day) => CancellationEffectiveOn is not { } lastDay || day <= lastDay;

    /// <summary>
    /// Cancels the membership with one month's notice to the end of a month (D-048). There is
    /// no retention step. A second cancellation changes nothing. It can be undone until it takes
    /// effect (D-055); see <see cref="UndoCancellation"/>.
    /// <paramref name="now"/> comes from the injected clock, never the system clock (rule 2).
    /// </summary>
    public CancellationOutcome Cancel(Instant now, DateTimeZone businessZone)
    {
        if (CancelledAt is not null)
        {
            return CancellationOutcome.AlreadyCancelled;
        }

        CancelledAt = now;
        CancellationEffectiveOn = CancellationNotice.EffectiveDate(now, businessZone);
        _cancellations.Add(new CancellationRecord(CancellationRecordId.New(), Id, now, CancellationEffectiveOn.Value));
        return CancellationOutcome.Cancelled;
    }

    /// <summary>
    /// Whether the pending cancellation can still be undone on <paramref name="today"/>
    /// (Copenhagen): up to and including the effective date (D-055).
    /// </summary>
    public bool CanUndoCancellationOn(LocalDate today) =>
        CancellationEffectiveOn is { } lastDay && today <= lastDay;

    /// <summary>
    /// Undoes a pending cancellation, up to and including its effective date (D-055). The
    /// membership continues as if it was never cancelled; the cancellation record is kept with
    /// the instant it was undone. With nothing pending, nothing changes.
    /// <paramref name="now"/> comes from the injected clock, never the system clock (rule 2).
    /// </summary>
    public UndoCancellationOutcome UndoCancellation(Instant now, DateTimeZone businessZone)
    {
        if (CancelledAt is not { } cancelledAt || CancellationEffectiveOn is not { } effectiveOn)
        {
            return UndoCancellationOutcome.NothingPending;
        }

        if (!CanUndoCancellationOn(now.InZone(businessZone).Date))
        {
            return UndoCancellationOutcome.AlreadyEffective;
        }

        var record = _cancellations.LastOrDefault(c => c.UndoneAt is null && c.CancelledAt == cancelledAt);
        if (record is null)
        {
            // A cancellation made before the history existed; keep it in the history now.
            record = new CancellationRecord(CancellationRecordId.New(), Id, cancelledAt, effectiveOn);
            _cancellations.Add(record);
        }

        record.Undo(now);
        CancelledAt = null;
        CancellationEffectiveOn = null;
        return UndoCancellationOutcome.Undone;
    }

    /// <summary>
    /// Moves paid-until to the end of a confirmed payment's period. The first payment may
    /// cover any period, also one in the past; later ones must follow on without a gap or
    /// an overlap, because those cases are not decided yet. The first payment activates
    /// the membership.
    /// </summary>
    public PaymentOutcome ApplyPayment(PaymentPeriod period)
    {
        if (PaidUntil is { } paidUntil && period.From != paidUntil.PlusDays(1))
        {
            return PaymentOutcome.NotContiguous;
        }

        PaidUntil = period.To;
        Status = MemberStatus.Active;
        return PaymentOutcome.Applied;
    }
}

/// <summary>What the member ticked on the signup form, with the text versions they were shown.</summary>
internal sealed record SignupConsents(
    bool TermsAccepted, string TermsVersion, bool EarlyStartRequested, string EarlyStartVersion);
