using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

internal enum MemberStatus
{
    /// <summary>Signed up; no payment confirmation yet (D-032). Check-in is refused (D-020).</summary>
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

/// <summary>
/// A person's membership at the center. Name and email live on the Identity account
/// (data-model.md); the member links to it by account id.
/// </summary>
internal sealed class Member
{
    private readonly List<Consent> _consents = [];

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

    public MemberStatus Status { get; private set; }

    /// <summary>Last day covered by a payment confirmation (D-020). Null until the first one.</summary>
    public LocalDate? PaidUntil { get; private set; }

    public IReadOnlyList<Consent> Consents => _consents;

    /// <summary>
    /// Signs a person up. The terms must be accepted; starting early is optional.
    /// <paramref name="now"/> comes from the injected clock, never the system clock (rule 2).
    /// </summary>
    public static Member SignUp(
        Guid accountId, MembershipType membershipType, SignupConsents consents, Instant now, DateTimeZone businessZone)
    {
        if (!consents.TermsAccepted)
        {
            throw new InvalidOperationException("The terms must be accepted to sign up.");
        }

        var member = new Member(MemberId.New(), accountId, membershipType.Id, now.InZone(businessZone).Date);

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
