using NodaTime;

namespace FitnessCenter.Modules.Members.Domain;

internal enum ConsentKind
{
    /// <summary>The member accepted the membership terms.</summary>
    Terms,

    /// <summary>
    /// The member expressly asked to start before the 14-day withdrawal period ends
    /// (Consumer Rights Directive Art. 7(3)/8(8), research/compliance-legal-security.md §6.1).
    /// </summary>
    EarlyStart,
}

internal enum ConsentChannel
{
    Online,
}

/// <summary>
/// Proof of what the member agreed to: which text version, when, and through which channel (D-032).
/// Never edited; a new text version means a new consent.
/// </summary>
internal sealed class Consent
{
    public Consent(ConsentId id, MemberId memberId, ConsentKind kind, string textVersion, Instant givenAt, ConsentChannel channel)
    {
        Id = id;
        MemberId = memberId;
        Kind = kind;
        TextVersion = textVersion;
        GivenAt = givenAt;
        Channel = channel;
    }

    public ConsentId Id { get; private set; }

    public MemberId MemberId { get; private set; }

    public ConsentKind Kind { get; private set; }

    public string TextVersion { get; private set; }

    public Instant GivenAt { get; private set; }

    public ConsentChannel Channel { get; private set; }
}
