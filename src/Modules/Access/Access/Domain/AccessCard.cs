using NodaTime;

namespace FitnessCenter.Modules.Access.Domain;

/// <summary>
/// A member's card for the entrance reader (D-003, data-model.md ACCESS_CARD). In the MVP
/// every card is simulated (D-021). Lost cards, replacements and blocking are still open,
/// so a card can't be deactivated yet.
/// </summary>
internal sealed class AccessCard
{
    private AccessCard(AccessCardId id, string uid, Guid memberId)
    {
        Id = id;
        Uid = uid;
        MemberId = memberId;
    }

    public AccessCardId Id { get; private set; }

    /// <summary>Unique across all cards, already normalized (<see cref="CardUid.Normalize"/>).</summary>
    public string Uid { get; private set; }

    /// <summary>The member the card belongs to (members schema; FK only, rule 3).</summary>
    public Guid MemberId { get; private set; }

    public static AccessCard Issue(Guid memberId, string uid)
    {
        var normalized = CardUid.Normalize(uid) ?? throw new ArgumentException("Not a card UID.", nameof(uid));
        return new AccessCard(AccessCardId.New(), normalized, memberId);
    }

    /// <summary>
    /// The card was swiped at <paramref name="at"/>, which comes from the injected clock (rule 2).
    /// </summary>
    public Attendance Swipe(Instant at) => new(AttendanceId.New(), Id, MemberId, at);
}
